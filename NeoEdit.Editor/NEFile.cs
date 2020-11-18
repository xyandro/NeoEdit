using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.TaskRunning;

namespace NeoEdit.Editor
{
	public partial class NEFile : INEFile
	{
		static ThreadSafeRandom random = new ThreadSafeRandom();

		public NEFile(string fileName = null, string displayName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, ParserType contentType = ParserType.None, bool? modified = null, int? line = null, int? column = null, int? index = null, ShutdownData shutdownData = null)
		{
			data = new NEFileData(this);

			Text = new NEText("");
			Selections = new List<Range>();
			for (var region = 1; region <= 9; ++region)
				SetRegions(region, new List<Range>());
			ViewBinaryCodePages = new HashSet<Coder.CodePage>(Coder.DefaultCodePages);
			StrictParsing = true;
			UndoRedo = new UndoRedo();

			fileName = fileName?.Trim('"');
			this.shutdownData = shutdownData;

			AutoRefresh = KeepSelections = HighlightSyntax = true;
			JumpBy = JumpByType.Words;

			OpenFile(fileName, displayName, bytes, codePage, contentType, modified);
			Goto(line, column, index);
		}

		public static NEFile CreateSummaryFile(string displayName, List<(string str, int count)> summary)
		{
			var sb = new StringBuilder();
			var countRanges = new List<Range>();
			var stringRanges = new List<Range>();
			foreach (var tuple in summary)
			{
				var countStr = tuple.count.ToString();
				countRanges.Add(Range.FromIndex(sb.Length, countStr.Length));
				sb.Append(countStr);

				sb.Append(" ");

				stringRanges.Add(Range.FromIndex(sb.Length, tuple.str.Length));
				sb.Append(tuple.str);

				if ((!tuple.str.EndsWith("\r")) && (!tuple.str.EndsWith("\n")))
					sb.Append("\r\n");
			}

			return new NEFile(displayName: displayName, bytes: Coder.StringToBytes(sb.ToString(), Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, modified: false) { Selections = stringRanges };
		}

		public string NEFileLabel => $"{DisplayName ?? (string.IsNullOrEmpty(FileName) ? "[Untitled]" : Path.GetFileName(FileName))}{(IsModified ? "*" : "")}{(DiffTarget != null ? $" (Diff{(CodePage != DiffTarget.CodePage ? " - Encoding mismatch" : "")})" : "")}";

		CacheValue modifiedChecksum = new CacheValue();
		CacheValue previousData = new CacheValue();
		ParserType previousType;
		ParserNode previousRoot;

		void ReplaceOneWithMany(IReadOnlyList<string> strs, bool? addNewLines)
		{
			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			var ending = addNewLines ?? strs.Any(str => !str.EndsWith(Text.DefaultEnding)) ? Text.DefaultEnding : "";
			if (ending.Length != 0)
				strs = strs.Select(str => str + ending).ToList();
			var ranges = new List<Range>();
			if (strs.Count > 0)
			{
				ranges.Add(Selections[0]);
				ranges.AddRange(Enumerable.Repeat(new Range(Selections[0].End), strs.Count - 1));
			}
			var position = Selections.Single().Start;
			Replace(ranges, strs);

			var sels = new List<Range>();
			foreach (var str in strs)
			{
				sels.Add(Range.FromIndex(position, str.Length - ending.Length));
				position += str.Length;
			}
			Selections = sels;
		}

		void ReplaceSelections(string str, bool highlight = true, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false) => ReplaceSelections(Enumerable.Repeat(str, Selections.Count).ToList(), highlight, replaceType, tryJoinUndo);

		void ReplaceSelections(IReadOnlyList<string> strs, bool highlight = true, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false)
		{
			Replace(Selections, strs, replaceType, tryJoinUndo);

			if (highlight)
				Selections = Selections.AsTaskRunner().Select((range, index) => new Range(range.End, range.End - (strs == null ? 0 : strs[index].Length))).ToList();
			else
				Selections = Selections.AsTaskRunner().Select(range => new Range(range.End)).ToList();
		}

		void Replace(IReadOnlyList<Range> ranges, IReadOnlyList<string> strs = null, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false)
		{
			if (strs == null)
				strs = Enumerable.Repeat("", ranges.Count).ToList();
			if (ranges.Count != strs.Count)
				throw new Exception("Invalid string count");

			var undoRanges = new List<Range>();
			var undoText = new List<string>();

			var change = 0;
			for (var ctr = 0; ctr < ranges.Count; ++ctr)
			{
				var undoRange = Range.FromIndex(ranges[ctr].Start + change, strs[ctr].Length);
				undoRanges.Add(undoRange);
				undoText.Add(Text.GetString(ranges[ctr]));
				change = undoRange.End - ranges[ctr].End;
			}

			var textCanvasUndoRedo = new UndoRedo.UndoRedoStep(undoRanges, undoText, tryJoinUndo);
			switch (replaceType)
			{
				case ReplaceType.Undo: UndoRedo = UndoRedo.AddUndone(textCanvasUndoRedo); break;
				case ReplaceType.Redo: UndoRedo = UndoRedo.AddRedone(textCanvasUndoRedo); break;
				case ReplaceType.Normal: UndoRedo = UndoRedo.AddUndo(textCanvasUndoRedo, IsModified); break;
			}

			Text = Text.Replace(ranges, strs);
			SetModifiedFlag();
			CalculateDiff();

			var translateMap = GetTranslateMap(ranges, strs, new List<IReadOnlyList<Range>> { Selections }.Concat(Enumerable.Range(1, 9).Select(region => GetRegions(region))).ToList());
			Selections = Translate(Selections, translateMap);
			for (var region = 1; region <= 9; ++region)
				SetRegions(region, Translate(GetRegions(region), translateMap));
		}

		#region Translate
		static int[] GetTranslateNums(IReadOnlyList<IReadOnlyList<Range>> rangeLists)
		{
			var nums = new int[rangeLists.Sum(rangeList => rangeList.Count * 2)];
			var numsStart = 0;
			foreach (var rangeList in rangeLists)
			{
				var size = Math.Max(65536, (rangeList.Count + 31) / 32);
				Helpers.PartitionedParallelForEach(rangeList.Count, size, (start, end) =>
				{
					var numPos = numsStart + start * 2;
					for (var r = start; r < end; ++r)
					{
						nums[numPos++] = rangeList[r].Start;
						nums[numPos++] = rangeList[r].End;
					}
				});
				numsStart += rangeList.Count * 2;
			}

			Array.Sort(nums);

			var outPos = -1;
			for (var inPos = 0; inPos < nums.Length; ++inPos)
			{
				if ((outPos != -1) && (nums[inPos] == nums[outPos]))
					continue;
				nums[++outPos] = nums[inPos];
			}

			Array.Resize(ref nums, outPos + 1);
			return nums;
		}

		static Tuple<int[], int[]> GetTranslateMap(IReadOnlyList<Range> replaceRanges, IReadOnlyList<string> strs, IReadOnlyList<IReadOnlyList<Range>> rangeLists)
		{
			var translateNums = GetTranslateNums(rangeLists);
			var translateResults = new int[translateNums.Length];
			var replaceRange = 0;
			var offset = 0;
			var current = 0;
			while (current < translateNums.Length)
			{
				int start = int.MaxValue, end = int.MaxValue, length = 0;
				if (replaceRange < replaceRanges.Count)
				{
					start = replaceRanges[replaceRange].Start;
					end = replaceRanges[replaceRange].End;
					length = strs[replaceRange].Length;
				}

				if (translateNums[current] >= end)
				{
					offset += start - end + length;
					++replaceRange;
					continue;
				}

				var value = translateNums[current];
				if ((value > start) && (value < end))
					value = start + length;

				translateResults[current] = value + offset;
				++current;
			}

			return Tuple.Create(translateNums, translateResults);
		}

		static IReadOnlyList<Range> Translate(IReadOnlyList<Range> ranges, Tuple<int[], int[]> translateMap)
		{
			var result = Helpers.PartitionedParallelForEach<Range>(ranges.Count, Math.Max(65536, (ranges.Count + 31) / 32), (start, end, list) =>
			{
				var current = 0;
				for (var ctr = start; ctr < end; ++ctr)
				{
					current = Array.IndexOf(translateMap.Item1, ranges[ctr].Start, current);
					var startPos = current;
					current = Array.IndexOf(translateMap.Item1, ranges[ctr].End, current);
					if (ranges[ctr].Cursor < ranges[ctr].Anchor)
						list.Add(new Range(translateMap.Item2[startPos], translateMap.Item2[current]));
					else
						list.Add(new Range(translateMap.Item2[current], translateMap.Item2[startPos]));
				}
			});
			return result;
		}
		#endregion

		#region DeOverlap
		enum DeOverlapStep
		{
			Sort,
			DeOverlap,
			Done,
		}

		static IReadOnlyList<Range> DeOverlap(IReadOnlyList<Range> items)
		{
			while (true)
			{
				switch (GetDeOverlapStep(items))
				{
					case DeOverlapStep.Sort: items = items.OrderBy(range => range.Start).ThenBy(range => range.End).ToList(); break;
					case DeOverlapStep.DeOverlap: return DoDeOverlap(items);
					case DeOverlapStep.Done: return items;
					default: throw new Exception("Invalid step");
				}
			}
		}

		static DeOverlapStep GetDeOverlapStep(IReadOnlyList<Range> items)
		{
			var result = DeOverlapStep.Done;
			for (var ctr = 1; ctr < items.Count; ++ctr)
			{
				if ((items[ctr].Start < items[ctr - 1].Start) || ((items[ctr].Start == items[ctr - 1].Start) && (items[ctr].End < items[ctr - 1].End)))
					return DeOverlapStep.Sort;

				if ((items[ctr].Start < items[ctr - 1].End) || ((items[ctr].Start == items[ctr - 1].Start) && (items[ctr].End == items[ctr - 1].End)))
					result = DeOverlapStep.DeOverlap;
			}

			return result;
		}

		static IReadOnlyList<Range> DoDeOverlap(IReadOnlyList<Range> items)
		{
			var result = new List<Range>();

			using (var enumerator = items.GetEnumerator())
			{
				var last = default(Range);

				while (true)
				{
					var range = enumerator.MoveNext() ? enumerator.Current : null;

					if ((last != null) && ((range == null) || (last.Start != range.Start)))
					{
						if ((range == null) || (last.End <= range.Start))
							result.Add(last);
						else if (last.Cursor < last.Anchor)
							result.Add(new Range(last.Start, range.Start));
						else
							result.Add(new Range(range.Start, last.Start));
						last = null;
					}

					if (range == null)
						break;

					if ((last != null) && (range.End <= last.End))
						continue;

					last = range;
				}
			}

			return result;
		}
		#endregion

		#region Configure
		public static void Configure()
		{
			var handled = true;
			switch (EditorExecuteState.CurrentState.Command)
			{
				case NECommand.File_Open_Open: Configure_FileMacro_Open_Open(); break;
				case NECommand.Macro_Open_Open: Configure_FileMacro_Open_Open(Macro.MacroDirectory); break;
				case NECommand.Window_CustomGrid: Configure_Window_CustomGrid(); break;
				default: handled = false; break;
			}

			if ((handled) || (EditorExecuteState.CurrentState.NEFiles.Focused == null))
				return;

			switch (EditorExecuteState.CurrentState.Command)
			{
				case NECommand.Internal_Key: Configure_Internal_Key(); break;
				case NECommand.File_Open_ReopenWithEncoding: Configure_File_Open_ReopenWithEncoding(); break;
				case NECommand.File_Save_SaveAsByExpression: Configure_File_SaveCopyAdvanced_SaveAsCopyByExpressionSetDisplayName(); break;
				case NECommand.File_Move_MoveByExpression: Configure_File_Move_MoveByExpression(); break;
				case NECommand.File_Copy_CopyByExpression: Configure_File_SaveCopyAdvanced_SaveAsCopyByExpressionSetDisplayName(); break;
				case NECommand.File_Encoding: Configure_File_Encoding(); break;
				case NECommand.File_LineEndings: Configure_File_LineEndings(); break;
				case NECommand.File_Advanced_Encrypt: Configure_File_Advanced_Encrypt(); break;
				case NECommand.File_Advanced_SetDisplayName: Configure_File_SaveCopyAdvanced_SaveAsCopyByExpressionSetDisplayName(); break;
				case NECommand.Edit_Select_Limit: Configure_Edit_Select_Limit(); break;
				case NECommand.Edit_Select_ToggleAnchor: Configure_Edit_Select_ToggleAnchor(); break;
				case NECommand.Edit_Paste_Paste: Configure_Edit_Paste_PasteRotatePaste(); break;
				case NECommand.Edit_Paste_RotatePaste: Configure_Edit_Paste_PasteRotatePaste(); break;
				case NECommand.Edit_Repeat: Configure_Edit_Repeat(); break;
				case NECommand.Edit_Rotate: Configure_Edit_Rotate(); break;
				case NECommand.Edit_Expression_Expression: Configure_Edit_Expression_Expression(); break;
				case NECommand.Edit_ModifyRegions: Configure_Edit_ModifyRegions(); break;
				case NECommand.Edit_Advanced_Convert: Configure_Edit_Advanced_Convert(); break;
				case NECommand.Edit_Advanced_Hash: Configure_Edit_Advanced_Hash(); break;
				case NECommand.Edit_Advanced_Compress: Configure_Edit_Advanced_Compress(); break;
				case NECommand.Edit_Advanced_Decompress: Configure_Edit_Advanced_Decompress(); break;
				case NECommand.Edit_Advanced_Encrypt: Configure_Edit_Advanced_Encrypt(); break;
				case NECommand.Edit_Advanced_Decrypt: Configure_Edit_Advanced_Decrypt(); break;
				case NECommand.Edit_Advanced_Sign: Configure_Edit_Advanced_Sign(); break;
				case NECommand.Text_Select_WholeWord: Configure_Text_Select_WholeBoundedWord(true); break;
				case NECommand.Text_Select_BoundedWord: Configure_Text_Select_WholeBoundedWord(false); break;
				case NECommand.Text_Select_Trim: Configure_Text_Select_Trim(); break;
				case NECommand.Text_Select_Split: Configure_Text_Select_Split(); break;
				case NECommand.Text_Select_Repeats_ByCount_IgnoreCase: Configure_Text_Select_Repeats_ByCount_IgnoreMatchCase(); break;
				case NECommand.Text_Select_Repeats_ByCount_MatchCase: Configure_Text_Select_Repeats_ByCount_IgnoreMatchCase(); break;
				case NECommand.Text_Select_ByWidth: Configure_Text_Select_ByWidth(); break;
				case NECommand.Text_Find_Find: Configure_Text_Find_Find(); break;
				case NECommand.Text_Find_RegexReplace: Configure_Text_Find_RegexReplace(); break;
				case NECommand.Text_Trim: Configure_Text_Trim(); break;
				case NECommand.Text_Width: Configure_Text_Width(); break;
				case NECommand.Text_Sort: Configure_Text_Sort(); break;
				case NECommand.Text_Random: Configure_Text_Random(); break;
				case NECommand.Text_Advanced_Unicode: Configure_Text_Advanced_Unicode(); break;
				case NECommand.Text_Advanced_FirstDistinct: Configure_Text_Advanced_FirstDistinct(); break;
				case NECommand.Text_Advanced_ReverseRegex: Configure_Text_Advanced_ReverseRegex(); break;
				case NECommand.Numeric_Select_Limit: Configure_Numeric_Select_Limit(); break;
				case NECommand.Numeric_Round: Configure_Numeric_Round(); break;
				case NECommand.Numeric_Floor: Configure_Numeric_Floor(); break;
				case NECommand.Numeric_Ceiling: Configure_Numeric_Ceiling(); break;
				case NECommand.Numeric_Scale: Configure_Numeric_Scale(); break;
				case NECommand.Numeric_Cycle: Configure_Numeric_Cycle(); break;
				case NECommand.Numeric_Series_Linear: Configure_Numeric_Series_LinearGeometric(true); break;
				case NECommand.Numeric_Series_Geometric: Configure_Numeric_Series_LinearGeometric(false); break;
				case NECommand.Numeric_ConvertBase_ConvertBase: Configure_Numeric_ConvertBase_ConvertBase(); break;
				case NECommand.Numeric_RandomNumber: Configure_Numeric_RandomNumber(); break;
				case NECommand.Numeric_CombinationsPermutations: Configure_Numeric_CombinationsPermutations(); break;
				case NECommand.Numeric_MinMaxValues: Configure_Numeric_MinMaxValues(); break;
				case NECommand.Files_Select_ByContent: Configure_Files_Select_ByContent(); break;
				case NECommand.Files_Select_BySourceControlStatus: Configure_Files_Select_BySourceControlStatus(); break;
				case NECommand.Files_Copy: Configure_Files_CopyMove(false); break;
				case NECommand.Files_Move: Configure_Files_CopyMove(true); break;
				case NECommand.Files_Name_MakeAbsolute: Configure_Files_Name_MakeAbsolute(); break;
				case NECommand.Files_Name_MakeRelative: Configure_Files_Name_MakeRelative(); break;
				case NECommand.Files_Get_Hash: Configure_Files_Get_Hash(); break;
				case NECommand.Files_Get_Content: Configure_Files_Get_Content(); break;
				case NECommand.Files_Set_Size: Configure_Files_Set_Size(); break;
				case NECommand.Files_Set_Time_Write: Configure_Files_Set_Time_Various(); break;
				case NECommand.Files_Set_Time_Access: Configure_Files_Set_Time_Various(); break;
				case NECommand.Files_Set_Time_Create: Configure_Files_Set_Time_Various(); break;
				case NECommand.Files_Set_Time_All: Configure_Files_Set_Time_Various(); break;
				case NECommand.Files_Set_Attributes: Configure_Files_Set_Attributes(); break;
				case NECommand.Files_Set_Content: Configure_Files_Set_Content(); break;
				case NECommand.Files_Set_Encoding: Configure_Files_Set_Encoding(); break;
				case NECommand.Files_Compress: Configure_Files_Compress(); break;
				case NECommand.Files_Decompress: Configure_Files_Decompress(); break;
				case NECommand.Files_Encrypt: Configure_Files_Encrypt(); break;
				case NECommand.Files_Decrypt: Configure_Files_Decrypt(); break;
				case NECommand.Files_Sign: Configure_Files_Sign(); break;
				case NECommand.Files_Advanced_SplitFiles: Configure_Files_Advanced_SplitFiles(); break;
				case NECommand.Files_Advanced_CombineFiles: Configure_Files_Advanced_CombineFiles(); break;
				case NECommand.Content_Ancestor: Configure_Content_Ancestor(); break;
				case NECommand.Content_Attributes: Configure_Content_Attributes(); break;
				case NECommand.Content_WithAttribute: Configure_Content_WithAttribute(); break;
				case NECommand.Content_Children_WithAttribute: Configure_Content_Children_WithAttribute(); break;
				case NECommand.Content_Descendants_WithAttribute: Configure_Content_Descendants_WithAttribute(); break;
				case NECommand.DateTime_ToTimeZone: Configure_DateTime_ToTimeZone(); break;
				case NECommand.DateTime_Format: Configure_DateTime_Format(); break;
				case NECommand.Table_Select_RowsByExpression: Configure_Table_Select_RowsByExpression(); break;
				case NECommand.Table_New_FromSelection: Configure_Table_New_FromSelection(); break;
				case NECommand.Table_Edit: Configure_Table_Edit(); break;
				case NECommand.Table_Convert: Configure_Table_Convert(); break;
				case NECommand.Table_Join: Configure_Table_Join(); break;
				case NECommand.Table_Database_GenerateInserts: Configure_Table_Database_GenerateInserts(); break;
				case NECommand.Table_Database_GenerateUpdates: Configure_Table_Database_GenerateUpdates(); break;
				case NECommand.Table_Database_GenerateDeletes: Configure_Table_Database_GenerateDeletes(); break;
				case NECommand.Image_Resize: Configure_Image_Resize(); break;
				case NECommand.Image_Crop: Configure_Image_Crop(); break;
				case NECommand.Image_GrabColor: Configure_Image_GrabColor(); break;
				case NECommand.Image_GrabImage: Configure_Image_GrabImage(); break;
				case NECommand.Image_AddColor: Configure_Image_AddOverlayColor(true); break;
				case NECommand.Image_AdjustColor: Configure_Image_AdjustColor(); break;
				case NECommand.Image_OverlayColor: Configure_Image_AddOverlayColor(false); break;
				case NECommand.Image_Rotate: Configure_Image_Rotate(); break;
				case NECommand.Image_GIF_Animate: Configure_Image_GIF_Animate(); break;
				case NECommand.Image_GIF_Split: Configure_Image_GIF_Split(); break;
				case NECommand.Image_SetTakenDate: Configure_Image_SetTakenDate(); break;
				case NECommand.Position_Goto_Lines: Configure_Position_Goto_Various(GotoType.Line); break;
				case NECommand.Position_Goto_Columns: Configure_Position_Goto_Various(GotoType.Column); break;
				case NECommand.Position_Goto_Indexes: Configure_Position_Goto_Various(GotoType.Index); break;
				case NECommand.Position_Goto_Positions: Configure_Position_Goto_Various(GotoType.Position); break;
				case NECommand.Diff_IgnoreCharacters: Configure_Diff_IgnoreCharacters(); break;
				case NECommand.Diff_Fix_Whitespace: Configure_Diff_Fix_Whitespace(); break;
				case NECommand.Network_AbsoluteURL: Configure_Network_AbsoluteURL(); break;
				case NECommand.Network_Fetch_File: Configure_Network_Fetch_File(); break;
				case NECommand.Network_Fetch_Stream: Configure_Network_Fetch_Stream(); break;
				case NECommand.Network_Fetch_Playlist: Configure_Network_Fetch_Playlist(); break;
				case NECommand.Network_Ping: Configure_Network_Ping(); break;
				case NECommand.Network_ScanPorts: Configure_Network_ScanPorts(); break;
				case NECommand.Network_WCF_GetConfig: Configure_Network_WCF_GetConfig(); break;
				case NECommand.Network_WCF_InterceptCalls: Configure_Network_WCF_InterceptCalls(); break;
				case NECommand.Database_Connect: Configure_Database_Connect(); break;
				case NECommand.Database_Examine: Configure_Database_Examine(); break;
				case NECommand.Window_BinaryCodePages: Configure_Window_BinaryCodePages(); break;
			}
		}
		#endregion

		#region PreExecute
		public static bool PreExecute()
		{
			switch (EditorExecuteState.CurrentState.Command)
			{
				case NECommand.Internal_Activate: return PreExecute_Internal_Activate();
				case NECommand.Internal_MouseActivate: return PreExecute_Internal_MouseActivate();
				case NECommand.Internal_CloseFile: return PreExecute_Internal_CloseFile();
				case NECommand.Internal_Key: return PreExecute_Internal_Key();
				case NECommand.Internal_Scroll: return PreExecute_Internal_Scroll();
				case NECommand.Internal_Mouse: return PreExecute_Internal_Mouse();
				case NECommand.File_Select_All: return PreExecute_File_Select_All();
				case NECommand.File_Select_None: return PreExecute_File_Select_None();
				case NECommand.File_Select_WithSelections: return PreExecute_File_Select_WithWithoutSelections(true);
				case NECommand.File_Select_WithoutSelections: return PreExecute_File_Select_WithWithoutSelections(false);
				case NECommand.File_Select_Modified: return PreExecute_File_Select_ModifiedUnmodified(true);
				case NECommand.File_Select_Unmodified: return PreExecute_File_Select_ModifiedUnmodified(false);
				case NECommand.File_Select_Inactive: return PreExecute_File_Select_Inactive();
				case NECommand.File_Select_Choose: return PreExecute_File_Select_Choose();
				case NECommand.File_New_New: return PreExecute_File_New_New();
				case NECommand.File_New_FromClipboard_Selections: return PreExecute_File_New_FromClipboard_Selections();
				case NECommand.File_New_FromClipboard_Files: return PreExecute_File_New_FromClipboard_Files();
				case NECommand.File_New_WordList: return PreExecute_File_New_WordList();
				case NECommand.File_Open_Open: return PreExecute_FileMacro_Open_Open();
				case NECommand.File_Open_CopiedCut: return PreExecute_File_Open_CopiedCut();
				case NECommand.File_Advanced_DontExitOnClose: return PreExecute_File_Advanced_DontExitOnClose();
				case NECommand.File_Close_ActiveFiles: return PreExecute_File_Close_ActiveInactiveFiles(true);
				case NECommand.File_Close_InactiveFiles: return PreExecute_File_Close_ActiveInactiveFiles(false);
				case NECommand.File_Exit: return PreExecute_File_Exit();
				case NECommand.Edit_Advanced_EscapeClearsSelections: return PreExecute_Edit_Advanced_EscapeClearsSelections();
				case NECommand.Text_Select_Repeats_BetweenFiles_Match_IgnoreCase: return PreExecute_Text_Select_Repeats_BetweenFiles_MatchMismatch_IgnoreMatchCase(false);
				case NECommand.Text_Select_Repeats_BetweenFiles_Match_MatchCase: return PreExecute_Text_Select_Repeats_BetweenFiles_MatchMismatch_IgnoreMatchCase(true);
				case NECommand.Text_Select_Repeats_BetweenFiles_Mismatch_IgnoreCase: return PreExecute_Text_Select_Repeats_BetweenFiles_MatchMismatch_IgnoreMatchCase(false);
				case NECommand.Text_Select_Repeats_BetweenFiles_Mismatch_MatchCase: return PreExecute_Text_Select_Repeats_BetweenFiles_MatchMismatch_IgnoreMatchCase(true);
				case NECommand.Text_Select_Repeats_BetweenFiles_Common_IgnoreCase: return PreExecute_Text_Select_Repeats_BetweenFiles_CommonNonCommon_IgnoreMatchCase(false);
				case NECommand.Text_Select_Repeats_BetweenFiles_Common_MatchCase: return PreExecute_Text_Select_Repeats_BetweenFiles_CommonNonCommon_IgnoreMatchCase(true);
				case NECommand.Text_Select_Repeats_BetweenFiles_NonCommon_IgnoreCase: return PreExecute_Text_Select_Repeats_BetweenFiles_CommonNonCommon_IgnoreMatchCase(false);
				case NECommand.Text_Select_Repeats_BetweenFiles_NonCommon_MatchCase: return PreExecute_Text_Select_Repeats_BetweenFiles_CommonNonCommon_IgnoreMatchCase(true);
				case NECommand.Files_Select_BySourceControlStatus: return PreExecute_Files_SelectGet_BySourceControlStatus();
				case NECommand.Files_Get_SourceControlStatus: return PreExecute_Files_SelectGet_BySourceControlStatus();
				case NECommand.Diff_Select_LeftFile: return PreExecute_Diff_Select_LeftRightBothFiles(true);
				case NECommand.Diff_Select_RightFile: return PreExecute_Diff_Select_LeftRightBothFiles(false);
				case NECommand.Diff_Select_BothFiles: return PreExecute_Diff_Select_LeftRightBothFiles(null);
				case NECommand.Diff_Diff: return PreExecute_Diff_Diff();
				case NECommand.Macro_Play_Quick_1: return PreExecute_Macro_Play_Quick(1);
				case NECommand.Macro_Play_Quick_2: return PreExecute_Macro_Play_Quick(2);
				case NECommand.Macro_Play_Quick_3: return PreExecute_Macro_Play_Quick(3);
				case NECommand.Macro_Play_Quick_4: return PreExecute_Macro_Play_Quick(4);
				case NECommand.Macro_Play_Quick_5: return PreExecute_Macro_Play_Quick(5);
				case NECommand.Macro_Play_Quick_6: return PreExecute_Macro_Play_Quick(6);
				case NECommand.Macro_Play_Quick_7: return PreExecute_Macro_Play_Quick(7);
				case NECommand.Macro_Play_Quick_8: return PreExecute_Macro_Play_Quick(8);
				case NECommand.Macro_Play_Quick_9: return PreExecute_Macro_Play_Quick(9);
				case NECommand.Macro_Play_Quick_10: return PreExecute_Macro_Play_Quick(10);
				case NECommand.Macro_Play_Quick_11: return PreExecute_Macro_Play_Quick(11);
				case NECommand.Macro_Play_Quick_12: return PreExecute_Macro_Play_Quick(12);
				case NECommand.Macro_Play_Play: return PreExecute_Macro_Play_Play();
				case NECommand.Macro_Play_Repeat: return PreExecute_Macro_Play_Repeat();
				case NECommand.Macro_Play_PlayOnCopiedFiles: return PreExecute_Macro_Play_PlayOnCopiedFiles();
				case NECommand.Macro_Record_Quick_1: return PreExecute_Macro_Record_Quick(1);
				case NECommand.Macro_Record_Quick_2: return PreExecute_Macro_Record_Quick(2);
				case NECommand.Macro_Record_Quick_3: return PreExecute_Macro_Record_Quick(3);
				case NECommand.Macro_Record_Quick_4: return PreExecute_Macro_Record_Quick(4);
				case NECommand.Macro_Record_Quick_5: return PreExecute_Macro_Record_Quick(5);
				case NECommand.Macro_Record_Quick_6: return PreExecute_Macro_Record_Quick(6);
				case NECommand.Macro_Record_Quick_7: return PreExecute_Macro_Record_Quick(7);
				case NECommand.Macro_Record_Quick_8: return PreExecute_Macro_Record_Quick(8);
				case NECommand.Macro_Record_Quick_9: return PreExecute_Macro_Record_Quick(9);
				case NECommand.Macro_Record_Quick_10: return PreExecute_Macro_Record_Quick(10);
				case NECommand.Macro_Record_Quick_11: return PreExecute_Macro_Record_Quick(11);
				case NECommand.Macro_Record_Quick_12: return PreExecute_Macro_Record_Quick(12);
				case NECommand.Macro_Record_Record: return PreExecute_Macro_Record_Record();
				case NECommand.Macro_Record_StopRecording: return PreExecute_Macro_Record_StopRecording();
				case NECommand.Macro_Append_Quick_1: return PreExecute_Macro_Append_Quick(1);
				case NECommand.Macro_Append_Quick_2: return PreExecute_Macro_Append_Quick(2);
				case NECommand.Macro_Append_Quick_3: return PreExecute_Macro_Append_Quick(3);
				case NECommand.Macro_Append_Quick_4: return PreExecute_Macro_Append_Quick(4);
				case NECommand.Macro_Append_Quick_5: return PreExecute_Macro_Append_Quick(5);
				case NECommand.Macro_Append_Quick_6: return PreExecute_Macro_Append_Quick(6);
				case NECommand.Macro_Append_Quick_7: return PreExecute_Macro_Append_Quick(7);
				case NECommand.Macro_Append_Quick_8: return PreExecute_Macro_Append_Quick(8);
				case NECommand.Macro_Append_Quick_9: return PreExecute_Macro_Append_Quick(9);
				case NECommand.Macro_Append_Quick_10: return PreExecute_Macro_Append_Quick(10);
				case NECommand.Macro_Append_Quick_11: return PreExecute_Macro_Append_Quick(11);
				case NECommand.Macro_Append_Quick_12: return PreExecute_Macro_Append_Quick(12);
				case NECommand.Macro_Append_Append: return PreExecute_Macro_Append_Append();
				case NECommand.Macro_Open_Quick_1: return PreExecute_Macro_Open_Quick(1);
				case NECommand.Macro_Open_Quick_2: return PreExecute_Macro_Open_Quick(2);
				case NECommand.Macro_Open_Quick_3: return PreExecute_Macro_Open_Quick(3);
				case NECommand.Macro_Open_Quick_4: return PreExecute_Macro_Open_Quick(4);
				case NECommand.Macro_Open_Quick_5: return PreExecute_Macro_Open_Quick(5);
				case NECommand.Macro_Open_Quick_6: return PreExecute_Macro_Open_Quick(6);
				case NECommand.Macro_Open_Quick_7: return PreExecute_Macro_Open_Quick(7);
				case NECommand.Macro_Open_Quick_8: return PreExecute_Macro_Open_Quick(8);
				case NECommand.Macro_Open_Quick_9: return PreExecute_Macro_Open_Quick(9);
				case NECommand.Macro_Open_Quick_10: return PreExecute_Macro_Open_Quick(10);
				case NECommand.Macro_Open_Quick_11: return PreExecute_Macro_Open_Quick(11);
				case NECommand.Macro_Open_Quick_12: return PreExecute_Macro_Open_Quick(12);
				case NECommand.Macro_Open_Open: return PreExecute_FileMacro_Open_Open();
				case NECommand.Macro_Visualize: return PreExecute_Macro_Visualize();
				case NECommand.Window_New_NewWindow: return PreExecute_Window_New_NewWindow();
				case NECommand.Window_New_FromSelections_AllSelections: return PreExecute_Window_New_FromSelections_AllSelections();
				case NECommand.Window_New_FromSelections_EachFile: return PreExecute_Window_New_FromSelections_EachFile();
				case NECommand.Window_New_SummarizeSelections_AllSelections_IgnoreCase: return PreExecute_Window_New_SummarizeSelections_AllSelectionsEachFile_IgnoreMatchCase(false, false);
				case NECommand.Window_New_SummarizeSelections_AllSelections_MatchCase: return PreExecute_Window_New_SummarizeSelections_AllSelectionsEachFile_IgnoreMatchCase(true, false);
				case NECommand.Window_New_SummarizeSelections_EachFile_IgnoreCase: return PreExecute_Window_New_SummarizeSelections_AllSelectionsEachFile_IgnoreMatchCase(false, true);
				case NECommand.Window_New_SummarizeSelections_EachFile_MatchCase: return PreExecute_Window_New_SummarizeSelections_AllSelectionsEachFile_IgnoreMatchCase(true, true);
				case NECommand.Window_New_FromClipboard_AllSelections: return PreExecute_Window_New_FromClipboard_AllSelections();
				case NECommand.Window_New_FromClipboard_EachFile: return PreExecute_Window_New_FromClipboard_EachFile();
				case NECommand.Window_New_FromActiveFiles: return PreExecute_Window_New_FromActiveFiles();
				case NECommand.Window_Full: return PreExecute_Window_Full();
				case NECommand.Window_Grid: return PreExecute_Window_Grid();
				case NECommand.Window_CustomGrid: return PreExecute_Window_CustomGrid();
				case NECommand.Window_ActiveOnly: return PreExecute_Window_ActiveOnly();
				case NECommand.Window_Font_Size: return PreExecute_Window_Font_Size();
				case NECommand.Window_Font_ShowSpecial: return PreExecute_Window_Font_ShowSpecial();
				case NECommand.Help_Tutorial: return PreExecute_Help_Tutorial();
				case NECommand.Help_Update: return PreExecute_Help_Update();
				case NECommand.Help_TimeNextAction: return PreExecute_Help_TimeNextAction();
				case NECommand.Help_Advanced_Shell_Integrate: return PreExecute_Help_Advanced_Shell_Integrate();
				case NECommand.Help_Advanced_Shell_Unintegrate: return PreExecute_Help_Advanced_Shell_Unintegrate();
				case NECommand.Help_Advanced_CopyCommandLine: return PreExecute_Help_Advanced_CopyCommandLine();
				case NECommand.Help_Advanced_Extract: return PreExecute_Help_Advanced_Extract();
				case NECommand.Help_Advanced_RunGC: return PreExecute_Help_Advanced_RunGC();
				case NECommand.Help_About: return PreExecute_Help_About();
			}

			return false;
		}
		#endregion

		#region Execute
		public void Execute()
		{
			switch (EditorExecuteState.CurrentState.Command)
			{
				case NECommand.Internal_Key: Execute_Internal_Key(); break;
				case NECommand.Internal_Text: Execute_Internal_Text(); break;
				case NECommand.Internal_SetBinaryValue: Execute_Internal_SetBinaryValue(); break;
				case NECommand.File_Open_ReopenWithEncoding: Execute_File_Open_ReopenWithEncoding(); break;
				case NECommand.File_Refresh: Execute_File_Refresh(); break;
				case NECommand.File_AutoRefresh: Execute_File_AutoRefresh(); break;
				case NECommand.File_Revert: Execute_File_Revert(); break;
				case NECommand.File_Save_SaveModified: Execute_File_Save_SaveModified(); break;
				case NECommand.File_Save_SaveAll: Execute_File_Save_SaveAll(); break;
				case NECommand.File_Save_SaveAs: Execute_File_SaveCopy_SaveAsCopy(); break;
				case NECommand.File_Save_SaveAsByExpression: Execute_File_SaveCopy_SaveAsCopyByExpression(); break;
				case NECommand.File_Move_Move: Execute_File_Move_Move(); break;
				case NECommand.File_Move_MoveByExpression: Execute_File_Move_MoveByExpression(); break;
				case NECommand.File_Copy_Copy: Execute_File_SaveCopy_SaveAsCopy(true); break;
				case NECommand.File_Copy_CopyByExpression: Execute_File_SaveCopy_SaveAsCopyByExpression(true); break;
				case NECommand.File_Copy_Path: Execute_File_Copy_Path(); break;
				case NECommand.File_Copy_Name: Execute_File_Copy_Name(); break;
				case NECommand.File_Copy_DisplayName: Execute_File_Copy_DisplayName(); break;
				case NECommand.File_Delete: Execute_File_Delete(); break;
				case NECommand.File_Encoding: Execute_File_Encoding(); break;
				case NECommand.File_LineEndings: Execute_File_LineEndings(); break;
				case NECommand.File_FileIndex: Execute_File_FileActiveFileIndex(false); break;
				case NECommand.File_ActiveFileIndex: Execute_File_FileActiveFileIndex(true); break;
				case NECommand.File_Advanced_Compress: Execute_File_Advanced_Compress(); break;
				case NECommand.File_Advanced_Encrypt: Execute_File_Advanced_Encrypt(); break;
				case NECommand.File_Advanced_Explore: Execute_File_Advanced_Explore(); break;
				case NECommand.File_Advanced_CommandPrompt: Execute_File_Advanced_CommandPrompt(); break;
				case NECommand.File_Advanced_DragDrop: Execute_File_Advanced_DragDrop(); break;
				case NECommand.File_Advanced_SetDisplayName: Execute_File_Advanced_SetDisplayName(); break;
				case NECommand.File_Close_FilesWithSelections: Execute_File_Close_FilesWithWithoutSelections(true); break;
				case NECommand.File_Close_FilesWithoutSelections: Execute_File_Close_FilesWithWithoutSelections(false); break;
				case NECommand.File_Close_ModifiedFiles: Execute_File_Close_ModifiedUnmodifiedFiles(true); break;
				case NECommand.File_Close_UnmodifiedFiles: Execute_File_Close_ModifiedUnmodifiedFiles(false); break;
				case NECommand.Edit_Select_All: Execute_Edit_Select_All(); break;
				case NECommand.Edit_Select_Nothing: Execute_Edit_Select_Nothing(); break;
				case NECommand.Edit_Select_Join: Execute_Edit_Select_Join(); break;
				case NECommand.Edit_Select_Invert: Execute_Edit_Select_Invert(); break;
				case NECommand.Edit_Select_Limit: Execute_Edit_Select_Limit(); break;
				case NECommand.Edit_Select_Lines: Execute_Edit_Select_Lines(); break;
				case NECommand.Edit_Select_WholeLines: Execute_Edit_Select_WholeLines(); break;
				case NECommand.Edit_Select_Empty: Execute_Edit_Select_EmptyNonEmpty(true); break;
				case NECommand.Edit_Select_NonEmpty: Execute_Edit_Select_EmptyNonEmpty(false); break;
				case NECommand.Edit_Select_ToggleAnchor: Execute_Edit_Select_ToggleAnchor(); break;
				case NECommand.Edit_Select_Focused_First: Execute_Edit_Select_Focused_First(); break;
				case NECommand.Edit_Select_Focused_Next: Execute_Edit_Select_Focused_NextPrevious(true); break;
				case NECommand.Edit_Select_Focused_Previous: Execute_Edit_Select_Focused_NextPrevious(false); break;
				case NECommand.Edit_Select_Focused_Single: Execute_Edit_Select_Focused_Single(); break;
				case NECommand.Edit_Select_Focused_Remove: Execute_Edit_Select_Focused_Remove(); break;
				case NECommand.Edit_Select_Focused_RemoveBeforeCurrent: Execute_Edit_Select_Focused_RemoveBeforeCurrent(); break;
				case NECommand.Edit_Select_Focused_RemoveAfterCurrent: Execute_Edit_Select_Focused_RemoveAfterCurrent(); break;
				case NECommand.Edit_Select_Focused_CenterVertically: Execute_Edit_Select_Focused_CenterVertically(); break;
				case NECommand.Edit_Select_Focused_Center: Execute_Edit_Select_Focused_Center(); break;
				case NECommand.Edit_Copy: Execute_Edit_CopyCut(false); break;
				case NECommand.Edit_Cut: Execute_Edit_CopyCut(true); break;
				case NECommand.Edit_Paste_Paste: Execute_Edit_Paste_PasteRotatePaste(EditorExecuteState.CurrentState.ShiftDown, false); break;
				case NECommand.Edit_Paste_RotatePaste: Execute_Edit_Paste_PasteRotatePaste(true, true); break;
				case NECommand.Edit_Undo: Execute_Edit_Undo(); break;
				case NECommand.Edit_Redo: Execute_Edit_Redo(); break;
				case NECommand.Edit_Repeat: Execute_Edit_Repeat(); break;
				case NECommand.Edit_Rotate: Execute_Edit_Rotate(); break;
				case NECommand.Edit_Expression_Expression: Execute_Edit_Expression_Expression(); break;
				case NECommand.Edit_Expression_EvaluateSelected: Execute_Edit_Expression_EvaluateSelected(); break;
				case NECommand.Edit_ModifyRegions: Execute_Edit_ModifyRegions_Various_Various_Region(EditorExecuteState.CurrentState.Configuration as Configuration_Edit_ModifyRegions); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region1: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Select, 1); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region2: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Select, 2); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region3: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Select, 3); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region4: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Select, 4); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region5: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Select, 5); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region6: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Select, 6); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region7: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Select, 7); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region8: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Select, 8); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region9: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Select, 9); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region1: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Previous, 1); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region2: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Previous, 2); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region3: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Previous, 3); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region4: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Previous, 4); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region5: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Previous, 5); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region6: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Previous, 6); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region7: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Previous, 7); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region8: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Previous, 8); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region9: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Previous, 9); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region1: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Next, 1); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region2: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Next, 2); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region3: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Next, 3); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region4: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Next, 4); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region5: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Next, 5); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region6: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Next, 6); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region7: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Next, 7); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region8: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Next, 8); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region9: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Next, 9); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region1: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Enclosing, 1); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region2: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Enclosing, 2); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region3: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Enclosing, 3); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region4: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Enclosing, 4); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region5: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Enclosing, 5); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region6: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Enclosing, 6); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region7: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Enclosing, 7); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region8: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Enclosing, 8); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region9: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_Enclosing, 9); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region1: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_WithEnclosing, 1); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region2: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_WithEnclosing, 2); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region3: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_WithEnclosing, 3); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region4: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_WithEnclosing, 4); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region5: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_WithEnclosing, 5); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region6: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_WithEnclosing, 6); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region7: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_WithEnclosing, 7); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region8: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_WithEnclosing, 8); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region9: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_WithEnclosing, 9); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region1: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_WithoutEnclosing, 1); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region2: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_WithoutEnclosing, 2); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region3: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_WithoutEnclosing, 3); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region4: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_WithoutEnclosing, 4); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region5: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_WithoutEnclosing, 5); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region6: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_WithoutEnclosing, 6); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region7: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_WithoutEnclosing, 7); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region8: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_WithoutEnclosing, 8); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region9: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Select_WithoutEnclosing, 9); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region1: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Set, 1); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region2: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Set, 2); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region3: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Set, 3); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region4: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Set, 4); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region5: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Set, 5); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region6: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Set, 6); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region7: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Set, 7); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region8: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Set, 8); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region9: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Set, 9); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region1: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Clear, 1); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region2: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Clear, 2); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region3: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Clear, 3); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region4: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Clear, 4); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region5: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Clear, 5); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region6: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Clear, 6); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region7: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Clear, 7); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region8: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Clear, 8); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region9: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Clear, 9); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region1: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Remove, 1); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region2: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Remove, 2); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region3: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Remove, 3); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region4: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Remove, 4); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region5: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Remove, 5); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region6: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Remove, 6); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region7: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Remove, 7); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region8: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Remove, 8); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region9: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Remove, 9); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region1: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Add, 1); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region2: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Add, 2); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region3: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Add, 3); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region4: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Add, 4); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region5: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Add, 5); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region6: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Add, 6); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region7: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Add, 7); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region8: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Add, 8); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region9: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Add, 9); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region1: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Unite, 1); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region2: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Unite, 2); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region3: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Unite, 3); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region4: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Unite, 4); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region5: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Unite, 5); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region6: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Unite, 6); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region7: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Unite, 7); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region8: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Unite, 8); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region9: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Unite, 9); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region1: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Intersect, 1); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region2: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Intersect, 2); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region3: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Intersect, 3); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region4: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Intersect, 4); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region5: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Intersect, 5); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region6: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Intersect, 6); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region7: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Intersect, 7); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region8: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Intersect, 8); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region9: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Intersect, 9); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region1: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Exclude, 1); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region2: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Exclude, 2); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region3: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Exclude, 3); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region4: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Exclude, 4); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region5: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Exclude, 5); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region6: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Exclude, 6); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region7: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Exclude, 7); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region8: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Exclude, 8); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region9: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Exclude, 9); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region1: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Repeat, 1); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region2: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Repeat, 2); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region3: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Repeat, 3); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region4: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Repeat, 4); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region5: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Repeat, 5); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region6: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Repeat, 6); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region7: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Repeat, 7); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region8: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Repeat, 8); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region9: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Modify_Repeat, 9); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region1: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Copy_Enclosing, 1); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region2: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Copy_Enclosing, 2); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region3: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Copy_Enclosing, 3); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region4: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Copy_Enclosing, 4); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region5: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Copy_Enclosing, 5); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region6: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Copy_Enclosing, 6); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region7: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Copy_Enclosing, 7); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region8: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Copy_Enclosing, 8); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region9: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Copy_Enclosing, 9); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region1: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Copy_EnclosingIndex, 1); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region2: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Copy_EnclosingIndex, 2); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region3: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Copy_EnclosingIndex, 3); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region4: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Copy_EnclosingIndex, 4); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region5: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Copy_EnclosingIndex, 5); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region6: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Copy_EnclosingIndex, 6); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region7: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Copy_EnclosingIndex, 7); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region8: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Copy_EnclosingIndex, 8); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region9: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Copy_EnclosingIndex, 9); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region1: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_Flatten, 1); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region2: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_Flatten, 2); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region3: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_Flatten, 3); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region4: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_Flatten, 4); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region5: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_Flatten, 5); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region6: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_Flatten, 6); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region7: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_Flatten, 7); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region8: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_Flatten, 8); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region9: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_Flatten, 9); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region1: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_Transpose, 1); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region2: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_Transpose, 2); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region3: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_Transpose, 3); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region4: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_Transpose, 4); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region5: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_Transpose, 5); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region6: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_Transpose, 6); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region7: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_Transpose, 7); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region8: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_Transpose, 8); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region9: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_Transpose, 9); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region1: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_RotateLeft, 1); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region2: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_RotateLeft, 2); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region3: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_RotateLeft, 3); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region4: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_RotateLeft, 4); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region5: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_RotateLeft, 5); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region6: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_RotateLeft, 6); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region7: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_RotateLeft, 7); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region8: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_RotateLeft, 8); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region9: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_RotateLeft, 9); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region1: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_RotateRight, 1); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region2: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_RotateRight, 2); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region3: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_RotateRight, 3); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region4: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_RotateRight, 4); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region5: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_RotateRight, 5); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region6: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_RotateRight, 6); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region7: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_RotateRight, 7); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region8: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_RotateRight, 8); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region9: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_RotateRight, 9); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region1: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_Rotate180, 1); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region2: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_Rotate180, 2); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region3: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_Rotate180, 3); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region4: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_Rotate180, 4); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region5: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_Rotate180, 5); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region6: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_Rotate180, 6); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region7: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_Rotate180, 7); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region8: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_Rotate180, 8); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region9: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_Rotate180, 9); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region1: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorHorizontal, 1); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region2: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorHorizontal, 2); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region3: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorHorizontal, 3); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region4: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorHorizontal, 4); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region5: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorHorizontal, 5); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region6: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorHorizontal, 6); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region7: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorHorizontal, 7); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region8: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorHorizontal, 8); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region9: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorHorizontal, 9); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region1: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorVertical, 1); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region2: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorVertical, 2); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region3: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorVertical, 3); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region4: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorVertical, 4); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region5: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorVertical, 5); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region6: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorVertical, 6); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region7: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorVertical, 7); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region8: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorVertical, 8); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region9: Execute_Edit_ModifyRegions_Various_Various_Region(Configuration_Edit_ModifyRegions.Actions.Transform_MirrorVertical, 9); break;
				case NECommand.Edit_Navigate_WordLeft: Execute_Edit_Navigate_WordLeftRight(false); break;
				case NECommand.Edit_Navigate_WordRight: Execute_Edit_Navigate_WordLeftRight(true); break;
				case NECommand.Edit_Navigate_AllLeft: Execute_Edit_Navigate_AllLeft(); break;
				case NECommand.Edit_Navigate_AllRight: Execute_Edit_Navigate_AllRight(); break;
				case NECommand.Edit_Navigate_JumpBy_Words: Execute_Edit_Navigate_JumpBy_Various(JumpByType.Words); break;
				case NECommand.Edit_Navigate_JumpBy_Numbers: Execute_Edit_Navigate_JumpBy_Various(JumpByType.Numbers); break;
				case NECommand.Edit_Navigate_JumpBy_Paths: Execute_Edit_Navigate_JumpBy_Various(JumpByType.Paths); break;
				case NECommand.Edit_RepeatCount: Execute_Edit_RepeatCount(); break;
				case NECommand.Edit_RepeatIndex: Execute_Edit_RepeatIndex(); break;
				case NECommand.Edit_Advanced_Convert: Execute_Edit_Advanced_Convert(); break;
				case NECommand.Edit_Advanced_Hash: Execute_Edit_Advanced_Hash(); break;
				case NECommand.Edit_Advanced_Compress: Execute_Edit_Advanced_Compress(); break;
				case NECommand.Edit_Advanced_Decompress: Execute_Edit_Advanced_Decompress(); break;
				case NECommand.Edit_Advanced_Encrypt: Execute_Edit_Advanced_Encrypt(); break;
				case NECommand.Edit_Advanced_Decrypt: Execute_Edit_Advanced_Decrypt(); break;
				case NECommand.Edit_Advanced_Sign: Execute_Edit_Advanced_Sign(); break;
				case NECommand.Edit_Advanced_RunCommand_Parallel: Execute_Edit_Advanced_RunCommand_Parallel(); break;
				case NECommand.Edit_Advanced_RunCommand_Sequential: Execute_Edit_Advanced_RunCommand_Sequential(); break;
				case NECommand.Edit_Advanced_RunCommand_Shell: Execute_Edit_Advanced_RunCommand_Shell(); break;
				case NECommand.Text_Select_WholeWord: Execute_Text_Select_WholeBoundedWord(true); break;
				case NECommand.Text_Select_BoundedWord: Execute_Text_Select_WholeBoundedWord(false); break;
				case NECommand.Text_Select_Trim: Execute_Text_Select_Trim(); break;
				case NECommand.Text_Select_Split: Execute_Text_Select_Split(); break;
				case NECommand.Text_Select_Repeats_Unique_IgnoreCase: Execute_Text_Select_Repeats_Unique_IgnoreMatchCase(false); break;
				case NECommand.Text_Select_Repeats_Unique_MatchCase: Execute_Text_Select_Repeats_Unique_IgnoreMatchCase(true); break;
				case NECommand.Text_Select_Repeats_Duplicates_IgnoreCase: Execute_Text_Select_Repeats_Duplicates_IgnoreMatchCase(false); break;
				case NECommand.Text_Select_Repeats_Duplicates_MatchCase: Execute_Text_Select_Repeats_Duplicates_IgnoreMatchCase(true); break;
				case NECommand.Text_Select_Repeats_NonMatchPrevious_IgnoreCase: Execute_Text_Select_Repeats_NonMatchPrevious_IgnoreMatchCase(false); break;
				case NECommand.Text_Select_Repeats_NonMatchPrevious_MatchCase: Execute_Text_Select_Repeats_NonMatchPrevious_IgnoreMatchCase(true); break;
				case NECommand.Text_Select_Repeats_MatchPrevious_IgnoreCase: Execute_Text_Select_Repeats_MatchPrevious_IgnoreMatchCase(false); break;
				case NECommand.Text_Select_Repeats_MatchPrevious_MatchCase: Execute_Text_Select_Repeats_MatchPrevious_IgnoreMatchCase(true); break;
				case NECommand.Text_Select_Repeats_ByCount_IgnoreCase: Execute_Text_Select_Repeats_ByCount_IgnoreMatchCase(false); break;
				case NECommand.Text_Select_Repeats_ByCount_MatchCase: Execute_Text_Select_Repeats_ByCount_IgnoreMatchCase(true); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Match_IgnoreCase: Execute_Text_Select_Repeats_BetweenFiles_MatchMismatch_IgnoreMatchCase(true); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Match_MatchCase: Execute_Text_Select_Repeats_BetweenFiles_MatchMismatch_IgnoreMatchCase(true); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Mismatch_IgnoreCase: Execute_Text_Select_Repeats_BetweenFiles_MatchMismatch_IgnoreMatchCase(false); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Mismatch_MatchCase: Execute_Text_Select_Repeats_BetweenFiles_MatchMismatch_IgnoreMatchCase(false); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Common_IgnoreCase: Execute_Text_Select_Repeats_BetweenFiles_CommonNonCommon_IgnoreMatchCase(true); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Common_MatchCase: Execute_Text_Select_Repeats_BetweenFiles_CommonNonCommon_IgnoreMatchCase(true); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_NonCommon_IgnoreCase: Execute_Text_Select_Repeats_BetweenFiles_CommonNonCommon_IgnoreMatchCase(false); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_NonCommon_MatchCase: Execute_Text_Select_Repeats_BetweenFiles_CommonNonCommon_IgnoreMatchCase(false); break;
				case NECommand.Text_Select_ByWidth: Execute_Text_Select_ByWidth(); break;
				case NECommand.Text_Select_Min_Text: Execute_Text_Select_MinMax_Text(false); break;
				case NECommand.Text_Select_Min_Length: Execute_Text_Select_MinMax_Length(false); break;
				case NECommand.Text_Select_Max_Text: Execute_Text_Select_MinMax_Text(true); break;
				case NECommand.Text_Select_Max_Length: Execute_Text_Select_MinMax_Length(true); break;
				case NECommand.Text_Select_ToggleOpenClose: Execute_Text_Select_ToggleOpenClose(); break;
				case NECommand.Text_Find_Find: Execute_Text_Find_Find(); break;
				case NECommand.Text_Find_RegexReplace: Execute_Text_Find_RegexReplace(); break;
				case NECommand.Text_Trim: Execute_Text_Trim(); break;
				case NECommand.Text_Width: Execute_Text_Width(); break;
				case NECommand.Text_SingleLine: Execute_Text_SingleLine(); break;
				case NECommand.Text_Case_Upper: Execute_Text_Case_Upper(); break;
				case NECommand.Text_Case_Lower: Execute_Text_Case_Lower(); break;
				case NECommand.Text_Case_Proper: Execute_Text_Case_Proper(); break;
				case NECommand.Text_Case_Invert: Execute_Text_Case_Invert(); break;
				case NECommand.Text_Sort: Execute_Text_Sort(); break;
				case NECommand.Text_Escape_Markup: Execute_Text_Escape_Markup(); break;
				case NECommand.Text_Escape_Regex: Execute_Text_Escape_Regex(); break;
				case NECommand.Text_Escape_URL: Execute_Text_Escape_URL(); break;
				case NECommand.Text_Unescape_Markup: Execute_Text_Unescape_Markup(); break;
				case NECommand.Text_Unescape_Regex: Execute_Text_Unescape_Regex(); break;
				case NECommand.Text_Unescape_URL: Execute_Text_Unescape_URL(); break;
				case NECommand.Text_Random: Execute_Text_Random(); break;
				case NECommand.Text_Advanced_Unicode: Execute_Text_Advanced_Unicode(); break;
				case NECommand.Text_Advanced_FirstDistinct: Execute_Text_Advanced_FirstDistinct(); break;
				case NECommand.Text_Advanced_GUID: Execute_Text_Advanced_GUID(); break;
				case NECommand.Text_Advanced_ReverseRegex: Execute_Text_Advanced_ReverseRegex(); break;
				case NECommand.Numeric_Select_Min: Execute_Numeric_Select_MinMax(false); break;
				case NECommand.Numeric_Select_Max: Execute_Numeric_Select_MinMax(true); break;
				case NECommand.Numeric_Select_Limit: Execute_Numeric_Select_Limit(); break;
				case NECommand.Numeric_Round: Execute_Numeric_Round(); break;
				case NECommand.Numeric_Floor: Execute_Numeric_Floor(); break;
				case NECommand.Numeric_Ceiling: Execute_Numeric_Ceiling(); break;
				case NECommand.Numeric_Sum_Sum: Execute_Numeric_Sum_Sum(); break;
				case NECommand.Numeric_Sum_Increment: Execute_Numeric_Sum_IncrementDecrement(true); break;
				case NECommand.Numeric_Sum_Decrement: Execute_Numeric_Sum_IncrementDecrement(false); break;
				case NECommand.Numeric_Sum_AddClipboard: Execute_Numeric_Sum_AddSubtractClipboard(true); break;
				case NECommand.Numeric_Sum_SubtractClipboard: Execute_Numeric_Sum_AddSubtractClipboard(false); break;
				case NECommand.Numeric_Sum_ForwardSum: Execute_Numeric_Sum_ForwardReverseSumWithUndo(true, false); break;
				case NECommand.Numeric_Sum_UndoForwardSum: Execute_Numeric_Sum_ForwardReverseSumWithUndo(true, true); break;
				case NECommand.Numeric_Sum_ReverseSum: Execute_Numeric_Sum_ForwardReverseSumWithUndo(false, false); break;
				case NECommand.Numeric_Sum_UndoReverseSum: Execute_Numeric_Sum_ForwardReverseSumWithUndo(false, true); break;
				case NECommand.Numeric_AbsoluteValue: Execute_Numeric_AbsoluteValue(); break;
				case NECommand.Numeric_Scale: Execute_Numeric_Scale(); break;
				case NECommand.Numeric_Cycle: Execute_Numeric_Cycle(); break;
				case NECommand.Numeric_Trim: Execute_Numeric_Trim(); break;
				case NECommand.Numeric_Fraction: Execute_Numeric_Fraction(); break;
				case NECommand.Numeric_Factor: Execute_Numeric_Factor(); break;
				case NECommand.Numeric_Series_ZeroBased: Execute_Numeric_Series_ZeroBased(); break;
				case NECommand.Numeric_Series_OneBased: Execute_Numeric_Series_OneBased(); break;
				case NECommand.Numeric_Series_Linear: Execute_Numeric_Series_LinearGeometric(true); break;
				case NECommand.Numeric_Series_Geometric: Execute_Numeric_Series_LinearGeometric(false); break;
				case NECommand.Numeric_ConvertBase_ToHex: Execute_Numeric_ConvertBase_ToHex(); break;
				case NECommand.Numeric_ConvertBase_FromHex: Execute_Numeric_ConvertBase_FromHex(); break;
				case NECommand.Numeric_ConvertBase_ConvertBase: Execute_Numeric_ConvertBase_ConvertBase(); break;
				case NECommand.Numeric_RandomNumber: Execute_Numeric_RandomNumber(); break;
				case NECommand.Numeric_CombinationsPermutations: Execute_Numeric_CombinationsPermutations(); break;
				case NECommand.Numeric_MinMaxValues: Execute_Numeric_MinMaxValues(); break;
				case NECommand.Files_Select_Files: Execute_Files_Select_Files(); break;
				case NECommand.Files_Select_Directories: Execute_Files_Select_Directories(); break;
				case NECommand.Files_Select_Existing: Execute_Files_Select_ExistingNonExisting(true); break;
				case NECommand.Files_Select_NonExisting: Execute_Files_Select_ExistingNonExisting(false); break;
				case NECommand.Files_Select_Name_Directory: Execute_Files_Select_Name_Various(GetPathType.Directory); break;
				case NECommand.Files_Select_Name_Name: Execute_Files_Select_Name_Various(GetPathType.FileName); break;
				case NECommand.Files_Select_Name_NameWOExtension: Execute_Files_Select_Name_Various(GetPathType.FileNameWoExtension); break;
				case NECommand.Files_Select_Name_Extension: Execute_Files_Select_Name_Various(GetPathType.Extension); break;
				case NECommand.Files_Select_Name_Next: Execute_Files_Select_Name_Next(); break;
				case NECommand.Files_Select_Name_CommonAncestor: Execute_Files_Select_Name_CommonAncestor(); break;
				case NECommand.Files_Select_Name_MatchDepth: Execute_Files_Select_Name_MatchDepth(); break;
				case NECommand.Files_Select_Roots: Execute_Files_Select_RootsNonRoots(true); break;
				case NECommand.Files_Select_NonRoots: Execute_Files_Select_RootsNonRoots(false); break;
				case NECommand.Files_Select_ByContent: Execute_Files_Select_ByContent(); break;
				case NECommand.Files_Select_BySourceControlStatus: Execute_Files_Select_BySourceControlStatus(); break;
				case NECommand.Files_Copy: Execute_Files_CopyMove(false); break;
				case NECommand.Files_Move: Execute_Files_CopyMove(true); break;
				case NECommand.Files_Delete: Execute_Files_Delete(); break;
				case NECommand.Files_Name_MakeAbsolute: Execute_Files_Name_MakeAbsolute(); break;
				case NECommand.Files_Name_MakeRelative: Execute_Files_Name_MakeRelative(); break;
				case NECommand.Files_Name_Simplify: Execute_Files_Name_Simplify(); break;
				case NECommand.Files_Name_Sanitize: Execute_Files_Name_Sanitize(); break;
				case NECommand.Files_Get_Size: Execute_Files_Get_Size(); break;
				case NECommand.Files_Get_Time_Write: Execute_Files_Get_Time_Various(TimestampType.Write); break;
				case NECommand.Files_Get_Time_Access: Execute_Files_Get_Time_Various(TimestampType.Access); break;
				case NECommand.Files_Get_Time_Create: Execute_Files_Get_Time_Various(TimestampType.Create); break;
				case NECommand.Files_Get_Attributes: Execute_Files_Get_Attributes(); break;
				case NECommand.Files_Get_Version_File: Execute_Files_Get_Version_File(); break;
				case NECommand.Files_Get_Version_Product: Execute_Files_Get_Version_Product(); break;
				case NECommand.Files_Get_Version_Assembly: Execute_Files_Get_Version_Assembly(); break;
				case NECommand.Files_Get_Hash: Execute_Files_Get_Hash(); break;
				case NECommand.Files_Get_SourceControlStatus: Execute_Files_Get_SourceControlStatus(); break;
				case NECommand.Files_Get_Children: Execute_Files_Get_ChildrenDescendants(false); break;
				case NECommand.Files_Get_Descendants: Execute_Files_Get_ChildrenDescendants(true); break;
				case NECommand.Files_Get_Content: Execute_Files_Get_Content(); break;
				case NECommand.Files_Set_Size: Execute_Files_Set_Size(); break;
				case NECommand.Files_Set_Time_Write: Execute_Files_Set_Time_Various(TimestampType.Write); break;
				case NECommand.Files_Set_Time_Access: Execute_Files_Set_Time_Various(TimestampType.Access); break;
				case NECommand.Files_Set_Time_Create: Execute_Files_Set_Time_Various(TimestampType.Create); break;
				case NECommand.Files_Set_Time_All: Execute_Files_Set_Time_Various(TimestampType.All); break;
				case NECommand.Files_Set_Attributes: Execute_Files_Set_Attributes(); break;
				case NECommand.Files_Set_Content: Execute_Files_Set_Content(); break;
				case NECommand.Files_Set_Encoding: Execute_Files_Set_Encoding(); break;
				case NECommand.Files_Create_Files: Execute_Files_Create_Files(); break;
				case NECommand.Files_Create_Directories: Execute_Files_Create_Directories(); break;
				case NECommand.Files_Compress: Execute_Files_Compress(); break;
				case NECommand.Files_Decompress: Execute_Files_Decompress(); break;
				case NECommand.Files_Encrypt: Execute_Files_Encrypt(); break;
				case NECommand.Files_Decrypt: Execute_Files_Decrypt(); break;
				case NECommand.Files_Sign: Execute_Files_Sign(); break;
				case NECommand.Files_Advanced_Explore: Execute_Files_Advanced_Explore(); break;
				case NECommand.Files_Advanced_CommandPrompt: Execute_Files_Advanced_CommandPrompt(); break;
				case NECommand.Files_Advanced_DragDrop: Execute_Files_Advanced_DragDrop(); break;
				case NECommand.Files_Advanced_SplitFiles: Execute_Files_Advanced_SplitFiles(); break;
				case NECommand.Files_Advanced_CombineFiles: Execute_Files_Advanced_CombineFiles(); break;
				case NECommand.Content_Type_SetFromExtension: Execute_Content_Type_SetFromExtension(); break;
				case NECommand.Content_Type_None: Execute_Content_Type_Various(ParserType.None); break;
				case NECommand.Content_Type_Balanced: Execute_Content_Type_Various(ParserType.Balanced); break;
				case NECommand.Content_Type_Columns: Execute_Content_Type_Various(ParserType.Columns); break;
				case NECommand.Content_Type_CPlusPlus: Execute_Content_Type_Various(ParserType.CPlusPlus); break;
				case NECommand.Content_Type_CSharp: Execute_Content_Type_Various(ParserType.CSharp); break;
				case NECommand.Content_Type_CSV: Execute_Content_Type_Various(ParserType.CSV); break;
				case NECommand.Content_Type_ExactColumns: Execute_Content_Type_Various(ParserType.ExactColumns); break;
				case NECommand.Content_Type_HTML: Execute_Content_Type_Various(ParserType.HTML); break;
				case NECommand.Content_Type_JSON: Execute_Content_Type_Various(ParserType.JSON); break;
				case NECommand.Content_Type_SQL: Execute_Content_Type_Various(ParserType.SQL); break;
				case NECommand.Content_Type_TSV: Execute_Content_Type_Various(ParserType.TSV); break;
				case NECommand.Content_Type_XML: Execute_Content_Type_Various(ParserType.XML); break;
				case NECommand.Content_HighlightSyntax: Execute_Content_HighlightSyntax(); break;
				case NECommand.Content_StrictParsing: Execute_Content_StrictParsing(); break;
				case NECommand.Content_Reformat: Execute_Content_Reformat(); break;
				case NECommand.Content_Comment: Execute_Content_Comment(); break;
				case NECommand.Content_Uncomment: Execute_Content_Uncomment(); break;
				case NECommand.Content_Copy: Execute_Content_Copy(); break;
				case NECommand.Content_TogglePosition: Execute_Content_TogglePosition(); break;
				case NECommand.Content_Current: Execute_Content_Current(); break;
				case NECommand.Content_Parent: Execute_Content_Parent(); break;
				case NECommand.Content_Ancestor: Execute_Content_Ancestor(); break;
				case NECommand.Content_Attributes: Execute_Content_Attributes(); break;
				case NECommand.Content_WithAttribute: Execute_Content_WithAttribute(); break;
				case NECommand.Content_Children_Children: Execute_Content_Children_Children(); break;
				case NECommand.Content_Children_SelfAndChildren: Execute_Content_Children_SelfAndChildren(); break;
				case NECommand.Content_Children_First: Execute_Content_Children_First(); break;
				case NECommand.Content_Children_WithAttribute: Execute_Content_Children_WithAttribute(); break;
				case NECommand.Content_Descendants_Descendants: Execute_Content_Descendants_Descendants(); break;
				case NECommand.Content_Descendants_SelfAndDescendants: Execute_Content_Descendants_SelfAndDescendants(); break;
				case NECommand.Content_Descendants_First: Execute_Content_Descendants_First(); break;
				case NECommand.Content_Descendants_WithAttribute: Execute_Content_Descendants_WithAttribute(); break;
				case NECommand.Content_Navigate_Up: Execute_Content_Navigate_Various(ParserNode.ParserNavigationDirectionEnum.Up, EditorExecuteState.CurrentState.ShiftDown); break;
				case NECommand.Content_Navigate_Down: Execute_Content_Navigate_Various(ParserNode.ParserNavigationDirectionEnum.Down, EditorExecuteState.CurrentState.ShiftDown); break;
				case NECommand.Content_Navigate_Left: Execute_Content_Navigate_Various(ParserNode.ParserNavigationDirectionEnum.Left, EditorExecuteState.CurrentState.ShiftDown); break;
				case NECommand.Content_Navigate_Right: Execute_Content_Navigate_Various(ParserNode.ParserNavigationDirectionEnum.Right, EditorExecuteState.CurrentState.ShiftDown); break;
				case NECommand.Content_Navigate_Home: Execute_Content_Navigate_Various(ParserNode.ParserNavigationDirectionEnum.Home, EditorExecuteState.CurrentState.ShiftDown); break;
				case NECommand.Content_Navigate_End: Execute_Content_Navigate_Various(ParserNode.ParserNavigationDirectionEnum.End, EditorExecuteState.CurrentState.ShiftDown); break;
				case NECommand.Content_Navigate_Pgup: Execute_Content_Navigate_Various(ParserNode.ParserNavigationDirectionEnum.PgUp, EditorExecuteState.CurrentState.ShiftDown); break;
				case NECommand.Content_Navigate_Pgdn: Execute_Content_Navigate_Various(ParserNode.ParserNavigationDirectionEnum.PgDn, EditorExecuteState.CurrentState.ShiftDown); break;
				case NECommand.Content_Navigate_Row: Execute_Content_Navigate_Various(ParserNode.ParserNavigationDirectionEnum.Row, true); break;
				case NECommand.Content_Navigate_Column: Execute_Content_Navigate_Various(ParserNode.ParserNavigationDirectionEnum.Column, true); break;
				case NECommand.Content_KeepSelections: Execute_Content_KeepSelections(); break;
				case NECommand.DateTime_Now: Execute_DateTime_Now(); break;
				case NECommand.DateTime_UTCNow: Execute_DateTime_UTCNow(); break;
				case NECommand.DateTime_ToUTC: Execute_DateTime_ToUTC(); break;
				case NECommand.DateTime_ToLocal: Execute_DateTime_ToLocal(); break;
				case NECommand.DateTime_ToTimeZone: Execute_DateTime_ToTimeZone(); break;
				case NECommand.DateTime_Format: Execute_DateTime_Format(); break;
				case NECommand.DateTime_AddClipboard: Execute_DateTime_AddClipboard(); break;
				case NECommand.DateTime_SubtractClipboard: Execute_DateTime_SubtractClipboard(); break;
				case NECommand.Table_Select_RowsByExpression: Execute_Table_Select_RowsByExpression(); break;
				case NECommand.Table_New_FromSelection: Execute_Table_New_FromSelection(); break;
				case NECommand.Table_New_FromLineSelections: Execute_Table_New_FromLineSelections(); break;
				case NECommand.Table_New_FromRegionSelections_Region1: Execute_Table_New_FromRegionSelections_Region(1); break;
				case NECommand.Table_New_FromRegionSelections_Region2: Execute_Table_New_FromRegionSelections_Region(2); break;
				case NECommand.Table_New_FromRegionSelections_Region3: Execute_Table_New_FromRegionSelections_Region(3); break;
				case NECommand.Table_New_FromRegionSelections_Region4: Execute_Table_New_FromRegionSelections_Region(4); break;
				case NECommand.Table_New_FromRegionSelections_Region5: Execute_Table_New_FromRegionSelections_Region(5); break;
				case NECommand.Table_New_FromRegionSelections_Region6: Execute_Table_New_FromRegionSelections_Region(6); break;
				case NECommand.Table_New_FromRegionSelections_Region7: Execute_Table_New_FromRegionSelections_Region(7); break;
				case NECommand.Table_New_FromRegionSelections_Region8: Execute_Table_New_FromRegionSelections_Region(8); break;
				case NECommand.Table_New_FromRegionSelections_Region9: Execute_Table_New_FromRegionSelections_Region(9); break;
				case NECommand.Table_Edit: Execute_Table_Edit(); break;
				case NECommand.Table_DetectType: Execute_Table_DetectType(); break;
				case NECommand.Table_Convert: Execute_Table_Convert(); break;
				case NECommand.Table_SetJoinSource: Execute_Table_SetJoinSource(); break;
				case NECommand.Table_Join: Execute_Table_Join(); break;
				case NECommand.Table_Transpose: Execute_Table_Transpose(); break;
				case NECommand.Table_Database_GenerateInserts: Execute_Table_Database_GenerateInserts(); break;
				case NECommand.Table_Database_GenerateUpdates: Execute_Table_Database_GenerateUpdates(); break;
				case NECommand.Table_Database_GenerateDeletes: Execute_Table_Database_GenerateDeletes(); break;
				case NECommand.Image_Resize: Execute_Image_Resize(); break;
				case NECommand.Image_Crop: Execute_Image_Crop(); break;
				case NECommand.Image_GrabColor: Execute_Image_GrabColor(); break;
				case NECommand.Image_GrabImage: Execute_Image_GrabImage(); break;
				case NECommand.Image_AddColor: Execute_Image_AddColor(); break;
				case NECommand.Image_AdjustColor: Execute_Image_AdjustColor(); break;
				case NECommand.Image_OverlayColor: Execute_Image_OverlayColor(); break;
				case NECommand.Image_FlipHorizontal: Execute_Image_FlipHorizontal(); break;
				case NECommand.Image_FlipVertical: Execute_Image_FlipVertical(); break;
				case NECommand.Image_Rotate: Execute_Image_Rotate(); break;
				case NECommand.Image_GIF_Animate: Execute_Image_GIF_Animate(); break;
				case NECommand.Image_GIF_Split: Execute_Image_GIF_Split(); break;
				case NECommand.Image_GetTakenDate: Execute_Image_GetTakenDate(); break;
				case NECommand.Image_SetTakenDate: Execute_Image_SetTakenDate(); break;
				case NECommand.Position_Goto_Lines: Execute_Position_Goto_Various(GotoType.Line, EditorExecuteState.CurrentState.ShiftDown); break;
				case NECommand.Position_Goto_Columns: Execute_Position_Goto_Various(GotoType.Column, EditorExecuteState.CurrentState.ShiftDown); break;
				case NECommand.Position_Goto_Indexes: Execute_Position_Goto_Various(GotoType.Index, EditorExecuteState.CurrentState.ShiftDown); break;
				case NECommand.Position_Goto_Positions: Execute_Position_Goto_Various(GotoType.Position, EditorExecuteState.CurrentState.ShiftDown); break;
				case NECommand.Position_Copy_Lines: Execute_Position_Copy_Various(GotoType.Line, false); break;
				case NECommand.Position_Copy_Columns: Execute_Position_Copy_Various(GotoType.Column, !EditorExecuteState.CurrentState.ShiftDown); break;
				case NECommand.Position_Copy_Indexes: Execute_Position_Copy_Various(GotoType.Index, !EditorExecuteState.CurrentState.ShiftDown); break;
				case NECommand.Position_Copy_Positions: Execute_Position_Copy_Various(GotoType.Position, false); break;
				case NECommand.Diff_Select_Matches: Execute_Diff_Select_MatchesDiffs(true); break;
				case NECommand.Diff_Select_Diffs: Execute_Diff_Select_MatchesDiffs(false); break;
				case NECommand.Diff_Break: Execute_Diff_Break(); break;
				case NECommand.Diff_SourceControl: Execute_Diff_SourceControl(); break;
				case NECommand.Diff_IgnoreWhitespace: Execute_Diff_IgnoreWhitespace(EditorExecuteState.CurrentState.MultiStatus); break;
				case NECommand.Diff_IgnoreCase: Execute_Diff_IgnoreCase(EditorExecuteState.CurrentState.MultiStatus); break;
				case NECommand.Diff_IgnoreNumbers: Execute_Diff_IgnoreNumbers(EditorExecuteState.CurrentState.MultiStatus); break;
				case NECommand.Diff_IgnoreLineEndings: Execute_Diff_IgnoreLineEndings(EditorExecuteState.CurrentState.MultiStatus); break;
				case NECommand.Diff_IgnoreCharacters: Execute_Diff_IgnoreCharacters(); break;
				case NECommand.Diff_Reset: Execute_Diff_Reset(); break;
				case NECommand.Diff_Next: Execute_Diff_NextPrevious(true, EditorExecuteState.CurrentState.ShiftDown); break;
				case NECommand.Diff_Previous: Execute_Diff_NextPrevious(false, EditorExecuteState.CurrentState.ShiftDown); break;
				case NECommand.Diff_CopyLeft: Execute_Diff_CopyLeftRight(true); break;
				case NECommand.Diff_CopyRight: Execute_Diff_CopyLeftRight(false); break;
				case NECommand.Diff_Fix_Whitespace: Execute_Diff_Fix_Whitespace(); break;
				case NECommand.Diff_Fix_Case: Execute_Diff_Fix_Case(); break;
				case NECommand.Diff_Fix_Numbers: Execute_Diff_Fix_Numbers(); break;
				case NECommand.Diff_Fix_LineEndings: Execute_Diff_Fix_LineEndings(); break;
				case NECommand.Diff_Fix_Encoding: Execute_Diff_Fix_Encoding(); break;
				case NECommand.Network_AbsoluteURL: Execute_Network_AbsoluteURL(); break;
				case NECommand.Network_Fetch_Fetch: Execute_Network_Fetch_FetchHex(); break;
				case NECommand.Network_Fetch_Hex: Execute_Network_Fetch_FetchHex(Coder.CodePage.Hex); break;
				case NECommand.Network_Fetch_File: Execute_Network_Fetch_File(); break;
				case NECommand.Network_Fetch_Stream: Execute_Network_Fetch_Stream(); break;
				case NECommand.Network_Fetch_Playlist: Execute_Network_Fetch_Playlist(); break;
				case NECommand.Network_Lookup_IP: Execute_Network_Lookup_IP(); break;
				case NECommand.Network_Lookup_Hostname: Execute_Network_Lookup_Hostname(); break;
				case NECommand.Network_AdaptersInfo: Execute_Network_AdaptersInfo(); break;
				case NECommand.Network_Ping: Execute_Network_Ping(); break;
				case NECommand.Network_ScanPorts: Execute_Network_ScanPorts(); break;
				case NECommand.Network_WCF_GetConfig: Execute_Network_WCF_GetConfig(); break;
				case NECommand.Network_WCF_Execute: Execute_Network_WCF_Execute(); break;
				case NECommand.Network_WCF_InterceptCalls: Execute_Network_WCF_InterceptCalls(); break;
				case NECommand.Network_WCF_ResetClients: Execute_Network_WCF_ResetClients(); break;
				case NECommand.Database_Connect: Execute_Database_Connect(); break;
				case NECommand.Database_ExecuteQuery: Execute_Database_ExecuteQuery(); break;
				case NECommand.Database_GetSproc: Execute_Database_GetSproc(); break;
				case NECommand.KeyValue_Set_Keys_IgnoreCase: Execute_KeyValue_Set_KeysValues_IgnoreMatchCase(0, false); break;
				case NECommand.KeyValue_Set_Keys_MatchCase: Execute_KeyValue_Set_KeysValues_IgnoreMatchCase(0, true); break;
				case NECommand.KeyValue_Set_Values1: Execute_KeyValue_Set_KeysValues_IgnoreMatchCase(1); break;
				case NECommand.KeyValue_Set_Values2: Execute_KeyValue_Set_KeysValues_IgnoreMatchCase(2); break;
				case NECommand.KeyValue_Set_Values3: Execute_KeyValue_Set_KeysValues_IgnoreMatchCase(3); break;
				case NECommand.KeyValue_Set_Values4: Execute_KeyValue_Set_KeysValues_IgnoreMatchCase(4); break;
				case NECommand.KeyValue_Set_Values5: Execute_KeyValue_Set_KeysValues_IgnoreMatchCase(5); break;
				case NECommand.KeyValue_Set_Values6: Execute_KeyValue_Set_KeysValues_IgnoreMatchCase(6); break;
				case NECommand.KeyValue_Set_Values7: Execute_KeyValue_Set_KeysValues_IgnoreMatchCase(7); break;
				case NECommand.KeyValue_Set_Values8: Execute_KeyValue_Set_KeysValues_IgnoreMatchCase(8); break;
				case NECommand.KeyValue_Set_Values9: Execute_KeyValue_Set_KeysValues_IgnoreMatchCase(9); break;
				case NECommand.KeyValue_Add_Keys: Execute_KeyValue_Add_KeysValues(0); break;
				case NECommand.KeyValue_Add_Values1: Execute_KeyValue_Add_KeysValues(1); break;
				case NECommand.KeyValue_Add_Values2: Execute_KeyValue_Add_KeysValues(2); break;
				case NECommand.KeyValue_Add_Values3: Execute_KeyValue_Add_KeysValues(3); break;
				case NECommand.KeyValue_Add_Values4: Execute_KeyValue_Add_KeysValues(4); break;
				case NECommand.KeyValue_Add_Values5: Execute_KeyValue_Add_KeysValues(5); break;
				case NECommand.KeyValue_Add_Values6: Execute_KeyValue_Add_KeysValues(6); break;
				case NECommand.KeyValue_Add_Values7: Execute_KeyValue_Add_KeysValues(7); break;
				case NECommand.KeyValue_Add_Values8: Execute_KeyValue_Add_KeysValues(8); break;
				case NECommand.KeyValue_Add_Values9: Execute_KeyValue_Add_KeysValues(9); break;
				case NECommand.KeyValue_Remove_Keys: Execute_KeyValue_Remove_KeysValues(0); break;
				case NECommand.KeyValue_Remove_Values1: Execute_KeyValue_Remove_KeysValues(1); break;
				case NECommand.KeyValue_Remove_Values2: Execute_KeyValue_Remove_KeysValues(2); break;
				case NECommand.KeyValue_Remove_Values3: Execute_KeyValue_Remove_KeysValues(3); break;
				case NECommand.KeyValue_Remove_Values4: Execute_KeyValue_Remove_KeysValues(4); break;
				case NECommand.KeyValue_Remove_Values5: Execute_KeyValue_Remove_KeysValues(5); break;
				case NECommand.KeyValue_Remove_Values6: Execute_KeyValue_Remove_KeysValues(6); break;
				case NECommand.KeyValue_Remove_Values7: Execute_KeyValue_Remove_KeysValues(7); break;
				case NECommand.KeyValue_Remove_Values8: Execute_KeyValue_Remove_KeysValues(8); break;
				case NECommand.KeyValue_Remove_Values9: Execute_KeyValue_Remove_KeysValues(9); break;
				case NECommand.KeyValue_Replace_Values1: Execute_KeyValue_Replace_Values(1); break;
				case NECommand.KeyValue_Replace_Values2: Execute_KeyValue_Replace_Values(2); break;
				case NECommand.KeyValue_Replace_Values3: Execute_KeyValue_Replace_Values(3); break;
				case NECommand.KeyValue_Replace_Values4: Execute_KeyValue_Replace_Values(4); break;
				case NECommand.KeyValue_Replace_Values5: Execute_KeyValue_Replace_Values(5); break;
				case NECommand.KeyValue_Replace_Values6: Execute_KeyValue_Replace_Values(6); break;
				case NECommand.KeyValue_Replace_Values7: Execute_KeyValue_Replace_Values(7); break;
				case NECommand.KeyValue_Replace_Values8: Execute_KeyValue_Replace_Values(8); break;
				case NECommand.KeyValue_Replace_Values9: Execute_KeyValue_Replace_Values(9); break;
				case NECommand.Window_Binary: Execute_Window_Binary(); break;
				case NECommand.Window_BinaryCodePages: Execute_Window_BinaryCodePages(); break;
			}
		}
		#endregion

		#region Watcher
		bool watcherFileModified = false;
		FileSystemWatcher watcher = null;
		DateTime fileLastWrite { get; set; }

		public void Activated()
		{
			if (!watcherFileModified)
				return;

			watcherFileModified = false;
			Execute_File_Refresh();
		}

		void ClearWatcher()
		{
			watcher?.Dispose();
			watcher = null;
		}

		void SetAutoRefresh(bool? value = null)
		{
			ClearWatcher();

			if (value.HasValue)
				AutoRefresh = value.Value;
			if ((!AutoRefresh) || (!File.Exists(FileName)))
				return;

			watcher = new FileSystemWatcher
			{
				Path = Path.GetDirectoryName(FileName),
				NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime,
				Filter = Path.GetFileName(FileName),
			};
			watcher.Changed += (s1, e1) =>
			{
				watcherFileModified = true;
				try { EditorExecuteState.CurrentState.NEFiles.FilesWindow.QueueActivateNEFiles(); } catch { }
			};
			watcher.EnableRaisingEvents = true;
		}
		#endregion

		void SetModifiedFlag(bool? newValue = null)
		{
			if (newValue.HasValue)
			{
				if (newValue == false)
					modifiedChecksum.SetValue(Text);
				else
					modifiedChecksum.Invalidate(); // Nothing will match, file will be perpetually modified
			}
			IsModified = !modifiedChecksum.Match(Text);
		}

		public IReadOnlyList<string> GetSelectionStrings() => Selections.AsTaskRunner().Select(range => Text.GetString(range)).ToList();

		void EnsureVisible(bool centerVertically = false, bool centerHorizontally = false)
		{
			CurrentSelection = Math.Max(0, Math.Min(CurrentSelection, Selections.Count - 1));
			if (!Selections.Any())
				return;

			var range = Selections[CurrentSelection];
			var lineMin = Text.GetPositionLine(range.Start);
			var lineMax = Text.GetPositionLine(range.End);
			var indexMin = Text.GetPositionIndex(range.Start, lineMin);
			var indexMax = Text.GetPositionIndex(range.End, lineMax);

			if (centerVertically)
			{
				StartRow = (lineMin + lineMax - EditorExecuteState.CurrentState.NEFiles.DisplayRows) / 2;
				if (centerHorizontally)
					StartColumn = (Text.GetColumnFromIndex(lineMin, indexMin) + Text.GetColumnFromIndex(lineMax, indexMax) - EditorExecuteState.CurrentState.NEFiles.DisplayColumns) / 2;
				else
					StartColumn = 0;
			}

			var line = Text.GetPositionLine(range.Cursor);
			var index = Text.GetPositionIndex(range.Cursor, line);
			var x = Text.GetColumnFromIndex(line, index);
			StartRow = Math.Min(line, Math.Max(line - (EditorExecuteState.CurrentState?.NEFiles?.DisplayRows ?? 1) + 1, StartRow));
			StartColumn = Math.Min(x, Math.Max(x - (EditorExecuteState.CurrentState?.NEFiles?.DisplayColumns ?? 1) + 1, StartColumn));
		}

		public NEVariables GetVariables()
		{
			// Can't access DependencyProperties/clipboard from other threads; grab a copy:
			var results = new NEVariables();

			var strs = default(List<string>);
			var initializeStrs = new NEVariableInitializer(() => strs = Selections.Select(range => Text.GetString(range)).ToList());
			results.Add(NEVariable.List("x", "Selection", () => strs, initializeStrs));
			results.Add(NEVariable.Constant("xn", "Selection count", () => Selections.Count));
			results.Add(NEVariable.List("xl", "Selection length", () => Selections.Select(range => range.Length)));
			results.Add(NEVariable.Constant("xlmin", "Selection min length", () => Selections.Select(range => range.Length).DefaultIfEmpty(0).Min()));
			results.Add(NEVariable.Constant("xlmax", "Selection max length", () => Selections.Select(range => range.Length).DefaultIfEmpty(0).Max()));

			results.Add(NEVariable.Constant("xmin", "Selection numeric min", () => Selections.AsTaskRunner().Select(range => Text.GetString(range)).Distinct().Select(str => double.Parse(str)).DefaultIfEmpty(0).Min()));
			results.Add(NEVariable.Constant("xmax", "Selection numeric max", () => Selections.AsTaskRunner().Select(range => Text.GetString(range)).Distinct().Select(str => double.Parse(str)).DefaultIfEmpty(0).Max()));

			results.Add(NEVariable.Constant("xtmin", "Selection text min", () => Selections.AsTaskRunner().Select(range => Text.GetString(range)).DefaultIfEmpty("").OrderBy(Helpers.SmartComparer(false)).First()));
			results.Add(NEVariable.Constant("xtmax", "Selection text max", () => Selections.AsTaskRunner().Select(range => Text.GetString(range)).DefaultIfEmpty("").OrderBy(Helpers.SmartComparer(false)).Last()));

			for (var ctr = 1; ctr <= 9; ++ctr)
			{
				var region = ctr; // If we don't copy this variable it passes the most recent value (10 after it's done) to GetRegions
				var regions = default(List<string>);
				var initializeRegions = new NEVariableInitializer(() => regions = GetRegions(region).Select(range => Text.GetString(range)).ToList());
				results.Add(NEVariable.List($"r{region}", $"Region {region}", () => regions, initializeRegions));
				results.Add(NEVariable.Constant($"r{region}n", $"Region {region} count", () => GetRegions(region).Count));
				results.Add(NEVariable.List($"r{region}l", $"Region {region} length", () => GetRegions(region).Select(range => range.Length)));
				results.Add(NEVariable.Constant($"r{region}lmin", $"Region {region} min length", () => GetRegions(region).Select(range => range.Length).DefaultIfEmpty(0).Min()));
				results.Add(NEVariable.Constant($"r{region}lmax", $"Region {region} max length", () => GetRegions(region).Select(range => range.Length).DefaultIfEmpty(0).Max()));
			}

			results.Add(NEVariable.Series("y", "One-based index", index => index + 1));
			results.Add(NEVariable.Series("z", "Zero-based index", index => index));

			if (Clipboard.Count == 1)
			{
				results.Add(NEVariable.Constant("c", "Clipboard", () => Clipboard[0]));
				results.Add(NEVariable.Constant("cl", "Clipboard length", () => Clipboard[0].Length));
				results.Add(NEVariable.Constant("clmin", "Clipboard min length", () => Clipboard[0].Length));
				results.Add(NEVariable.Constant("clmax", "Clipboard max length", () => Clipboard[0].Length));
			}
			else
			{
				results.Add(NEVariable.List("c", "Clipboard", () => Clipboard));
				results.Add(NEVariable.List("cl", "Clipboard length", () => Clipboard.Select(str => str.Length)));
				results.Add(NEVariable.Constant("clmin", "Clipboard min length", () => Clipboard.Select(str => str.Length).DefaultIfEmpty(0).Min()));
				results.Add(NEVariable.Constant("clmax", "Clipboard max length", () => Clipboard.Select(str => str.Length).DefaultIfEmpty(0).Max()));
			}
			results.Add(NEVariable.Constant("cn", "Clipboard count", () => Clipboard.Count));

			results.Add(NEVariable.Constant("f", "Filename", () => FileName));
			results.Add(NEVariable.Constant("d", "Display name", () => DisplayName));

			var lineStarts = default(IReadOnlyList<int>);
			var initializeLineStarts = new NEVariableInitializer(() => lineStarts = Selections.AsTaskRunner().Select(range => Text.GetPositionLine(range.Start) + 1).ToList());
			results.Add(NEVariable.List("line", "Selection line start", () => lineStarts, initializeLineStarts));
			var lineEnds = default(IReadOnlyList<int>);
			var initializeLineEnds = new NEVariableInitializer(() => lineEnds = Selections.AsTaskRunner().Select(range => Text.GetPositionLine(range.End) + 1).ToList());
			results.Add(NEVariable.List("lineend", "Selection line end", () => lineEnds, initializeLineEnds));

			var colStarts = default(IReadOnlyList<int>);
			var initializeColStarts = new NEVariableInitializer(() => colStarts = Selections.AsTaskRunner().Select((range, index) => Text.GetPositionIndex(range.Start, lineStarts[index] - 1) + 1).ToList(), initializeLineStarts);
			results.Add(NEVariable.List("col", "Selection column start", () => colStarts, initializeColStarts));
			var colEnds = default(IReadOnlyList<int>);
			var initializeColEnds = new NEVariableInitializer(() => colEnds = Selections.AsTaskRunner().Select((range, index) => Text.GetPositionIndex(range.End, lineEnds[index] - 1) + 1).ToList(), initializeLineEnds);
			results.Add(NEVariable.List("colend", "Selection column end", () => colEnds, initializeColEnds));

			var posStarts = default(IReadOnlyList<int>);
			var initializePosStarts = new NEVariableInitializer(() => posStarts = Selections.Select(range => range.Start).ToList());
			results.Add(NEVariable.List("pos", "Selection position start", () => posStarts, initializePosStarts));
			var posEnds = default(IReadOnlyList<int>);
			var initializePosEnds = new NEVariableInitializer(() => posEnds = Selections.Select(range => range.End).ToList());
			results.Add(NEVariable.List("posend", "Selection position end", () => posEnds, initializePosEnds));

			for (var ctr = 0; ctr < 10; ++ctr)
			{
				var name = ctr == 0 ? "k" : $"v{ctr}";
				var desc = ctr == 0 ? "Keys" : $"Values {ctr}";
				var values = GetKeysAndValues(ctr).Values;
				if (values == null)
					continue;
				results.Add(NEVariable.List(name, desc, () => values));
				results.Add(NEVariable.Constant($"{name}n", $"{desc} count", () => values.Count));
				results.Add(NEVariable.List($"{name}l", $"{desc} length", () => values.Select(str => str.Length)));
				results.Add(NEVariable.Constant($"{name}lmin", $"{desc} min length", () => values.Select(str => str.Length).DefaultIfEmpty(0).Min()));
				results.Add(NEVariable.Constant($"{name}lmax", $"{desc} max length", () => values.Select(str => str.Length).DefaultIfEmpty(0).Max()));
			}

			if (Coder.IsImage(CodePage))
			{
				results.Add(NEVariable.Constant("width", "Image width", () => GetBitmap().Width));
				results.Add(NEVariable.Constant("height", "Image height", () => GetBitmap().Height));
			}

			var nonNulls = default(IReadOnlyList<Tuple<double, int>>);
			double lineStart = 0, lineIncrement = 0, geoStart = 0, geoIncrement = 0;
			var initializeNonNulls = new NEVariableInitializer(() => nonNulls = Selections.AsTaskRunner().Select((range, index) => new { str = Text.GetString(range), index }).NonNullOrWhiteSpace(obj => obj.str).Select(obj => Tuple.Create(double.Parse(obj.str), obj.index)).ToList());
			var initializeLineSeries = new NEVariableInitializer(() =>
			{
				if (nonNulls.Count == 0)
					lineStart = lineIncrement = 1;
				else if (nonNulls.Count == 1)
				{
					lineStart = nonNulls[0].Item1;
					lineIncrement = 1;
				}
				else
				{
					var first = nonNulls.First();
					var last = nonNulls.Last();

					lineIncrement = (last.Item1 - first.Item1) / (last.Item2 - first.Item2);
					lineStart = first.Item1 - lineIncrement * first.Item2;
				}
			}, initializeNonNulls);
			var initializeGeoSeries = new NEVariableInitializer(() =>
			{
				if (nonNulls.Count == 0)
					geoStart = geoIncrement = 1;
				else if (nonNulls.Count == 1)
				{
					geoStart = nonNulls[0].Item1;
					geoIncrement = 1;
				}
				else
				{
					var first = nonNulls.First();
					var last = nonNulls.Last();

					geoIncrement = Math.Pow(last.Item1 / first.Item1, 1.0 / (last.Item2 - first.Item2));
					geoStart = first.Item1 / Math.Pow(geoIncrement, first.Item2);
				}
			}, initializeNonNulls);
			results.Add(NEVariable.Constant("linestart", "Linear series start", () => lineStart, initializeLineSeries));
			results.Add(NEVariable.Constant("lineincrement", "Linear series increment", () => lineIncrement, initializeLineSeries));
			results.Add(NEVariable.Constant("geostart", "Geometric series start", () => geoStart, initializeGeoSeries));
			results.Add(NEVariable.Constant("geoincrement", "Geometric series increment", () => geoIncrement, initializeGeoSeries));

			return results;
		}

		List<T> GetExpressionResults<T>(string expression, int? count = null) => EditorExecuteState.CurrentState.GetExpression(expression).EvaluateList<T>(GetVariables(), count);

		List<Range> GetEnclosingRegions(int useRegion, bool useAllRegions = false, bool mustBeInRegion = true)
		{
			var useRegions = GetRegions(useRegion);
			var regions = new List<Range>();
			var currentRegion = 0;
			var used = false;
			foreach (var selection in Selections)
			{
				while ((currentRegion < useRegions.Count) && (useRegions[currentRegion].End <= selection.Start))
				{
					if ((useAllRegions) && (!used))
						throw new Exception("Extra regions found.");
					used = false;
					++currentRegion;
				}
				if ((currentRegion < useRegions.Count) && (selection.Start >= useRegions[currentRegion].Start) && (selection.End <= useRegions[currentRegion].End))
				{
					regions.Add(useRegions[currentRegion]);
					used = true;
				}
				else if (mustBeInRegion)
					throw new Exception("No region found. All selections must be inside a region.");
				else
					regions.Add(null);
			}
			if ((Selections.Any()) && (useAllRegions) && (currentRegion != useRegions.Count - 1))
				throw new Exception("Extra regions found.");

			return regions;
		}

		static HashSet<string> drives = new HashSet<string>(DriveInfo.GetDrives().Select(drive => drive.Name));
		bool StringsAreFiles(IReadOnlyList<string> strs)
		{
			if ((strs.Count == 0) || (strs.Count > 500))
				return false;
			if (strs.Any(str => str.IndexOfAny(Path.GetInvalidPathChars()) != -1))
				return false;
			if (strs.Any(str => (!str.StartsWith("\\\\")) && (!drives.Any(drive => str.StartsWith(drive, StringComparison.OrdinalIgnoreCase)))))
				return false;
			if (strs.Any(str => !Helpers.FileOrDirectoryExists(str)))
				return false;
			return true;
		}

		bool CheckCanEncode(IEnumerable<byte[]> datas, Coder.CodePage codePage) => (datas.AsTaskRunner().All(data => Coder.CanEncode(data, codePage))) || (ConfirmContinueWhenCannotEncode());

		bool CheckCanEncode(IEnumerable<string> strs, Coder.CodePage codePage) => (strs.AsTaskRunner().All(str => Coder.CanEncode(str, codePage))) || (ConfirmContinueWhenCannotEncode());

		bool ConfirmContinueWhenCannotEncode()
		{
			return QueryUser(nameof(ConfirmContinueWhenCannotEncode), "The specified encoding cannot fully represent the data. Continue anyway?", MessageOptions.Yes);
		}

		DbConnection dbConnection { get; set; }

		void OpenTable(Table table, string name = null)
		{
			var contentType = ContentType.IsTableType() ? ContentType : ParserType.Columns;
			AddNewFile(new NEFile(displayName: name, bytes: Coder.StringToBytes(table.ToString("\r\n", contentType), Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, contentType: contentType, modified: false));
		}

		IReadOnlyList<string> RelativeSelectedFiles()
		{
			var fileName = FileName;
			return Selections.AsTaskRunner().Select(range => fileName.RelativeChild(Text.GetString(range))).ToList();
		}

		bool VerifyCanEncode()
		{
			if (Text.CanEncode(CodePage))
				return true;

			if (QueryUser(nameof(VerifyCanEncode), "The current encoding cannot fully represent this data. Switch to UTF-8?", MessageOptions.Yes))
				CodePage = Coder.CodePage.UTF8;
			return true;
		}

		void Save(string fileName, bool copyOnly = false)
		{
			if ((Coder.IsStr(CodePage)) && ((Text.Length >> 20) < 50) && (!VerifyCanEncode()))
				return;

			var triedReadOnly = false;
			while (true)
			{
				try
				{
					if ((!copyOnly) && (watcher != null))
						watcher.EnableRaisingEvents = false;
					File.WriteAllBytes(fileName, FileSaver.Encrypt(FileSaver.Compress(Text.GetBytes(CodePage), Compressed), AESKey));
					if ((!copyOnly) && (watcher != null))
						watcher.EnableRaisingEvents = true;
					break;
				}
				catch (UnauthorizedAccessException)
				{
					if ((triedReadOnly) || (!new FileInfo(fileName).IsReadOnly))
						throw;

					if (!QueryUser(nameof(Save), "Save failed. Remove read-only flag?", MessageOptions.Yes))
						throw;
					new FileInfo(fileName).IsReadOnly = false;
					triedReadOnly = true;
				}
			}

			if (!copyOnly)
			{
				fileLastWrite = new FileInfo(fileName).LastWriteTime;
				SetModifiedFlag(false);
				SetFileName(fileName);
			}
		}

		void SetFileName(string fileName)
		{
			if (FileName == fileName)
				return;

			FileName = fileName;
			ContentType = ParserExtensions.GetParserType(FileName);
			DisplayName = null;

			SetAutoRefresh();
		}

		public void VerifyCanClose()
		{
			if (!IsModified)
				return;

			if (QueryUser(nameof(VerifyCanClose), "Do you want to save changes?", MessageOptions.None))
				Execute_File_Save_SaveAll();
		}

		void OpenFile(string fileName, string displayName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, ParserType contentType = ParserType.None, bool? modified = null, bool keepUndo = false)
		{
			SetFileName(fileName);
			if (ContentType == ParserType.None)
				ContentType = contentType;
			DisplayName = displayName;
			var isModified = modified ?? bytes != null;
			if (bytes == null)
			{
				if (FileName == null)
					bytes = new byte[0];
				else
					bytes = File.ReadAllBytes(FileName);
			}

			FileSaver.HandleDecrypt(ref bytes, out var aesKey);
			AESKey = aesKey;

			bytes = FileSaver.Decompress(bytes, out var compressed);
			Compressed = compressed;

			if (codePage == Coder.CodePage.AutoByBOM)
				codePage = Coder.CodePageFromBOM(bytes);
			CodePage = codePage;

			var data = Coder.BytesToString(bytes, codePage, true);
			Replace(new List<Range> { Range.FromIndex(0, Text.Length) }, new List<string> { data });

			if (File.Exists(FileName))
				fileLastWrite = new FileInfo(FileName).LastWriteTime;

			// If encoding can't exactly express bytes mark as modified (only for < 50 MB)
			if ((!isModified) && ((bytes.Length >> 20) < 50))
				isModified = !Coder.CanExactlyEncode(bytes, CodePage);

			if (!keepUndo)
				UndoRedo = UndoRedo.Create();
			SetModifiedFlag(isModified);
		}

		public void Goto(int? line, int? column, int? index)
		{
			var pos = 0;
			if (line.HasValue)
			{
				var useLine = Math.Max(0, Math.Min(line.Value, Text.NumLines) - 1);
				int useIndex;
				if (column.HasValue)
					useIndex = Text.GetIndexFromColumn(useLine, Math.Max(0, column.Value - 1), true);
				else if (index.HasValue)
					useIndex = Math.Max(0, Math.Min(index.Value - 1, Text.GetLineLength(useLine)));
				else
					useIndex = 0;

				pos = Text.GetPosition(useLine, useIndex);
			}
			Selections = new List<Range> { new Range(pos) };
		}

		string savedBitmapText;
		System.Drawing.Bitmap savedBitmap;
		private ShutdownData shutdownData;

		System.Drawing.Bitmap GetBitmap()
		{
			if (!Coder.IsImage(CodePage))
			{
				savedBitmapText = null;
				savedBitmap = null;
			}
			else if (Text.GetString() != savedBitmapText)
			{
				savedBitmapText = Text.GetString();
				savedBitmap = Coder.StringToBitmap(Text.GetString());
			}
			return savedBitmap;
		}

		public bool Empty() => (FileName == null) && (!IsModified) && (Text.Length == 0);

		public void Closed()
		{
			if (DiffTarget != null)
				DiffTarget = null;
			ClearWatcher();
			shutdownData?.OnShutdown();
		}

		public override string ToString() => DisplayName ?? FileName;

		public void ViewGetBinaryData(out byte[] data, out bool hasSel)
		{
			if (Selections.Any())
			{
				var range = Selections[CurrentSelection];
				data = Coder.StringToBytes(Text.GetString(range.Start, Math.Min(range.HasSelection ? range.Length : 100, Text.Length - range.Start)), CodePage);
				hasSel = range.HasSelection;
			}
			else
			{
				data = null;
				hasSel = false;
			}
		}

		public void ViewSetDisplaySize(int columns, int rows) => EditorExecuteState.CurrentState.NEFiles?.SetDisplaySize(columns, rows);

		public List<string> ViewGetStatusBar()
		{
			var status = new List<string>();

			if (!Selections.Any())
			{
				status.Add("Selection 0/0");
				status.Add("Col");
				status.Add("In");
				status.Add("Pos");
			}
			else
			{
				var range = Selections[CurrentSelection];
				var startLine = Text.GetPositionLine(range.Start);
				var endLine = Text.GetPositionLine(range.End);
				var indexMin = Text.GetPositionIndex(range.Start, startLine);
				var indexMax = Text.GetPositionIndex(range.End, endLine);
				var lineMin = Text.GetDiffLine(startLine);
				var lineMax = Text.GetDiffLine(endLine);
				var columnMin = Text.GetColumnFromIndex(startLine, indexMin);
				var columnMax = Text.GetColumnFromIndex(endLine, indexMax);
				var posMin = range.Start;
				var posMax = range.End;

				status.Add($"Selection {CurrentSelection + 1:n0}/{Selections.Count:n0}");
				status.Add($"Col {lineMin + 1:n0}:{columnMin + 1:n0}{((lineMin == lineMax) && (columnMin == columnMax) ? "" : $"-{(lineMin == lineMax ? "" : $"{lineMax + 1:n0}:")}{columnMax + 1:n0}")}");
				status.Add($"In {lineMin + 1:n0}:{indexMin + 1:n0}{((lineMin == lineMax) && (indexMin == indexMax) ? "" : $"-{(lineMin == lineMax ? "" : $"{lineMax + 1:n0}:")}{indexMax + 1:n0}")}");
				status.Add($"Pos {posMin:n0}{(posMin == posMax ? "" : $"-{posMax:n0} ({posMax - posMin:n0})")}");
			}

			status.Add($"Regions {string.Join(" / ", Enumerable.Range(1, 9).Select(region => $"{GetRegions(region).Count:n0}"))}");
			status.Add($"Database {DBName}");

			return status;
		}

		public bool QueryUser(string name, string text, MessageOptions defaultAccept)
		{
			lock (EditorExecuteState.CurrentState)
			{
				if ((!EditorExecuteState.CurrentState.SavedAnswers[name].HasFlag(MessageOptions.All)) && (!EditorExecuteState.CurrentState.SavedAnswers[name].HasFlag(MessageOptions.Cancel)))
					EditorExecuteState.CurrentState.NEFiles.ShowFile(this, () => EditorExecuteState.CurrentState.SavedAnswers[name] = EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_ShowMessage("Confirm", text, MessageOptions.YesNoAllCancel, defaultAccept, MessageOptions.Cancel));
				if (EditorExecuteState.CurrentState.SavedAnswers[name] == MessageOptions.Cancel)
					throw new OperationCanceledException();
				return EditorExecuteState.CurrentState.SavedAnswers[name].HasFlag(MessageOptions.Yes);
			}
		}

		void CalculateDiff()
		{
			if (DiffTarget == null)
				return;

			DiffTarget.DiffIgnoreWhitespace = DiffIgnoreWhitespace;
			DiffTarget.DiffIgnoreCase = DiffIgnoreCase;
			DiffTarget.DiffIgnoreNumbers = DiffIgnoreNumbers;
			DiffTarget.DiffIgnoreLineEndings = DiffIgnoreLineEndings;
			DiffTarget.DiffIgnoreCharacters = DiffIgnoreCharacters;

			var left = EditorExecuteState.CurrentState.NEFiles?.GetFileIndex(this) < EditorExecuteState.CurrentState.NEFiles?.GetFileIndex(DiffTarget) ? this : DiffTarget;
			var right = left == this ? DiffTarget : this;
			NEText.CalculateDiff(left.Text, right.Text, DiffIgnoreWhitespace, DiffIgnoreCase, DiffIgnoreNumbers, DiffIgnoreLineEndings, DiffIgnoreCharacters);
		}

		public int ViewMaxColumn => Text.MaxColumn;

		public int ViewNumLines => Text.NumLines;

		public int ViewGetPosition(int line, int index, bool allowJustPastEnd = false) => Text.GetPosition(line, index, allowJustPastEnd);

		public int ViewGetIndexFromColumn(int line, int findColumn, bool returnMaxOnFail = false) => Text.GetIndexFromColumn(line, findColumn, returnMaxOnFail);

		public int ViewGetLineLength(int line) => Text.GetLineLength(line);

		public int ViewGetPositionLine(int position) => Text.GetPositionLine(position);

		public int ViewGetPositionIndex(int position, int line) => Text.GetPositionIndex(position, line);

		public int ViewGetColumnFromIndex(int line, int findIndex) => Text.GetColumnFromIndex(line, findIndex);

		public int ViewGetLineColumnsLength(int line) => Text.GetLineColumnsLength(line);

		public string ViewGetLineColumns(int line, int startColumn, int endColumn) => Text.GetLineColumns(line, startColumn, endColumn);

		public DiffType ViewGetLineDiffType(int line) => Text.GetLineDiffType(line);

		public List<int> ViewGetLineColumnMap(int line, bool includeEnding = false) => Text.GetLineColumnMap(line, includeEnding);

		public List<Tuple<int, int>> ViewGetLineColumnDiffs(int line) => Text.GetLineColumnDiffs(line);

		public List<Tuple<double, double>> ViewGetDiffRanges() => Text.GetDiffRanges();
	}
}
