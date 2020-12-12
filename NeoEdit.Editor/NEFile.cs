using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
		static EditorExecuteState state => EditorExecuteState.CurrentState;
		static int nextSerial;

		readonly NEText Text = new NEText("");
		readonly int serial = Interlocked.Increment(ref nextSerial);

		public DateTime LastActive { get; set; }
		public bool IsModified { get; private set; }
		public string DBName { get; private set; }
		int currentSelection;
		public int CurrentSelection { get => Math.Min(Math.Max(0, currentSelection), Selections.Count - 1); private set { currentSelection = value; } }

		public string DisplayName { get; private set; }
		public string FileName { get; private set; }
		public bool AutoRefresh { get; private set; }
		public bool AllowOverlappingSelections { get; private set; }
		public ParserType ContentType { get; set; }
		public Coder.CodePage CodePage { get; private set; }
		public bool HasBOM { get; private set; }
		public string AESKey { get; private set; }
		public bool Compressed { get; private set; }
		public bool DiffIgnoreWhitespace { get; private set; }
		public bool DiffIgnoreCase { get; private set; }
		public bool DiffIgnoreNumbers { get; private set; }
		public bool DiffIgnoreLineEndings { get; private set; }
		public string DiffIgnoreCharacters { get; private set; }
		public bool KeepSelections { get; private set; }
		public bool HighlightSyntax { get; private set; }
		public bool StrictParsing { get; private set; }
		public JumpByType JumpBy { get; private set; }
		public bool ViewBinary { get; private set; }
		public HashSet<Coder.CodePage> ViewBinaryCodePages { get; private set; }
		public IReadOnlyList<HashSet<string>> ViewBinarySearches { get; private set; }

		int startRow, startColumn;
		public int StartRow
		{
			get => startRow;
			private set
			{
				startRow = value;
				if (DiffTarget != null)
					DiffTarget.startRow = value;
			}
		}

		public int StartColumn
		{
			get => startColumn;
			private set
			{
				startColumn = value;
				if (DiffTarget != null)
					DiffTarget.startColumn = value;
			}
		}

		NEFile diffTarget;
		public NEFile DiffTarget
		{
			get => diffTarget;
			set
			{
				if (DiffTarget != null)
				{
					DiffTarget.NEWindow?.SetNeedsRender();
					Text.ClearDiff();
					DiffTarget.Text.ClearDiff();
					DiffTarget.diffTarget = null;
					diffTarget = null;
				}

				if (value != null)
				{
					value.DiffTarget = null;
					diffTarget = value;
					value.diffTarget = this;
					DiffTarget.NEWindow?.SetNeedsRender();
					CalculateDiff();
				}
			}
		}
		public NEFile(string fileName = null, string displayName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, ParserType contentType = ParserType.None, bool? modified = null, int? line = null, int? column = null, int? index = null)
		{
			Data = new NEFileData(this);

			Selections = new List<NERange>();
			for (var region = 1; region <= 9; ++region)
				SetRegions(region, new List<NERange>());
			ViewBinaryCodePages = new HashSet<Coder.CodePage>(Coder.DefaultCodePages);
			StrictParsing = true;

			if (fileName != null)
				fileName = Path.GetFullPath(fileName.Trim('"'));

			AutoRefresh = KeepSelections = HighlightSyntax = true;
			JumpBy = JumpByType.Words;

			OpenFile(fileName, displayName, bytes, codePage, contentType, modified);
			Goto(line, column, index);
		}

		public static NEFile CreateSummaryFile(string displayName, List<(string str, int count)> summary)
		{
			var sb = new StringBuilder();
			var countRanges = new List<NERange>();
			var stringRanges = new List<NERange>();
			foreach (var tuple in summary)
			{
				var countStr = tuple.count.ToString();
				countRanges.Add(NERange.FromIndex(sb.Length, countStr.Length));
				sb.Append(countStr);

				sb.Append(" ");

				stringRanges.Add(NERange.FromIndex(sb.Length, tuple.str.Length));
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
			var ranges = new List<NERange>();
			if (strs.Count > 0)
			{
				ranges.Add(Selections[0]);
				ranges.AddRange(Enumerable.Repeat(new NERange(Selections[0].End), strs.Count - 1));
			}
			var position = Selections.Single().Start;
			Replace(ranges, strs);

			var sels = new List<NERange>();
			foreach (var str in strs)
			{
				sels.Add(NERange.FromIndex(position, str.Length - ending.Length));
				position += str.Length;
			}
			Selections = sels;
		}

		void ReplaceSelections(string str, bool highlight = true) => ReplaceSelections(Enumerable.Repeat(str, Selections.Count).ToList(), highlight);

		void ReplaceSelections(IReadOnlyList<string> strs, bool highlight = true)
		{
			Replace(Selections, strs);

			if (highlight)
				Selections = Selections.AsTaskRunner().Select((range, index) => new NERange(range.End - (strs == null ? 0 : strs[index].Length), range.End)).ToList();
			else
				Selections = Selections.AsTaskRunner().Select(range => new NERange(range.End)).ToList();
		}

		void Replace(IReadOnlyList<NERange> ranges, IReadOnlyList<string> strs = null)
		{
			if (strs == null)
				strs = Enumerable.Repeat("", ranges.Count).ToList();
			if (ranges.Count != strs.Count)
				throw new Exception("Invalid string count");

			NETextPoint = Text.CreateTextPoint(ranges, strs);
			SetModifiedFlag();
			CalculateDiff();

			var translateMap = GetTranslateMap(ranges, strs, new List<IReadOnlyList<NERange>> { Selections }.Concat(Enumerable.Range(1, 9).Select(region => GetRegions(region))).ToList());
			Selections = Translate(Selections, translateMap);
			for (var region = 1; region <= 9; ++region)
				SetRegions(region, Translate(GetRegions(region), translateMap));
		}

		#region Translate
		static int[] GetTranslateNums(IReadOnlyList<IReadOnlyList<NERange>> rangeLists)
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

		static Tuple<int[], int[]> GetTranslateMap(IReadOnlyList<NERange> replaceRanges, IReadOnlyList<string> strs, IReadOnlyList<IReadOnlyList<NERange>> rangeLists)
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

		static IReadOnlyList<NERange> Translate(IReadOnlyList<NERange> ranges, Tuple<int[], int[]> translateMap)
		{
			var result = Helpers.PartitionedParallelForEach<NERange>(ranges.Count, Math.Max(65536, (ranges.Count + 31) / 32), (start, end, list) =>
			{
				var current = 0;
				for (var ctr = start; ctr < end; ++ctr)
				{
					current = Array.IndexOf(translateMap.Item1, ranges[ctr].Start, current);
					var startPos = current;
					current = Array.IndexOf(translateMap.Item1, ranges[ctr].End, current);
					if (ranges[ctr].Cursor < ranges[ctr].Anchor)
						list.Add(new NERange(translateMap.Item2[current], translateMap.Item2[startPos]));
					else
						list.Add(new NERange(translateMap.Item2[startPos], translateMap.Item2[current]));
				}
			});
			return result;
		}
		#endregion

		static bool NeedsSort(IReadOnlyList<NERange> items)
		{
			for (var ctr = 1; ctr < items.Count; ++ctr)
				if ((items[ctr].Start < items[ctr - 1].Start) || ((items[ctr].Start == items[ctr - 1].Start) && (items[ctr].End < items[ctr - 1].End)))
					return true;

			return false;
		}

		static IReadOnlyList<NERange> Sort(IReadOnlyList<NERange> items)
		{
			if (!NeedsSort(items))
				return items;
			return items.OrderBy(range => range.Start).ThenBy(range => range.End).ToList();
		}

		#region DeOverlap
		enum DeOverlapStep
		{
			Sort,
			DeOverlap,
			Done,
		}

		static IReadOnlyList<NERange> DeOverlap(IReadOnlyList<NERange> items)
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

		static DeOverlapStep GetDeOverlapStep(IReadOnlyList<NERange> items)
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

		static IReadOnlyList<NERange> DoDeOverlap(IReadOnlyList<NERange> items)
		{
			var result = new List<NERange>();

			using (var enumerator = items.GetEnumerator())
			{
				var last = default(NERange);

				while (true)
				{
					var range = enumerator.MoveNext() ? enumerator.Current : null;

					if ((last != null) && ((range == null) || (last.Start != range.Start)))
					{
						if ((range == null) || (last.End <= range.Start))
							result.Add(last);
						else if (last.Cursor < last.Anchor)
							result.Add(new NERange(range.Start, last.Start));
						else
							result.Add(new NERange(last.Start, range.Start));
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
			switch (state.Command)
			{
				case NECommand.Window_CustomGrid: Configure_Window_CustomGrid(); break;
				default: handled = false; break;
			}

			if ((handled) || (state.NEWindow.Focused == null))
				return;

			switch (state.Command)
			{
				case NECommand.File_Save_SaveAsByExpression: Configure_File_SaveCopyAdvanced_SaveAsCopyByExpressionSetDisplayName(); break;
				case NECommand.File_Move_MoveByExpression: Configure_File_Move_MoveByExpression(); break;
				case NECommand.File_Copy_CopyByExpression: Configure_File_SaveCopyAdvanced_SaveAsCopyByExpressionSetDisplayName(); break;
				case NECommand.File_Encoding: Configure_File_Encoding(); break;
				case NECommand.File_LineEndings: Configure_File_LineEndings(); break;
				case NECommand.File_Advanced_Encrypt: Configure_File_Advanced_Encrypt(); break;
				case NECommand.File_Advanced_SetDisplayName: Configure_File_SaveCopyAdvanced_SaveAsCopyByExpressionSetDisplayName(); break;
				case NECommand.File_Exit: Configure_File_Exit(); break;
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
			switch (state.Command)
			{
				case NECommand.Internal_Scroll: PreExecute_Internal_Scroll(); break;
				case NECommand.File_Advanced_DontExitOnClose: PreExecute_File_Advanced_DontExitOnClose(); break;
				case NECommand.File_Close_Active: PreExecute_File_Close_ActiveInactiveFiles(true); break;
				case NECommand.File_Close_Inactive: PreExecute_File_Close_ActiveInactiveFiles(false); break;
				case NECommand.File_Exit: PreExecute_File_Exit(); break;
				case NECommand.Edit_Undo_BetweenFiles_Text: PreExecute_Edit_Undo_BetweenFiles_Text(); break;
				case NECommand.Edit_Undo_BetweenFiles_Step: PreExecute_Edit_Undo_BetweenFiles_Step(); break;
				case NECommand.Edit_Undo_BetweenFiles_Sync: PreExecute_Edit_Undo_BetweenFiles_Sync(); break;
				case NECommand.Edit_Redo_BetweenFiles_Text: PreExecute_Edit_Redo_BetweenFiles_Text(); break;
				case NECommand.Edit_Redo_BetweenFiles_Step: PreExecute_Edit_Redo_BetweenFiles_Step(); break;
				case NECommand.Edit_Redo_BetweenFiles_Sync: PreExecute_Edit_Redo_BetweenFiles_Sync(); break;
				case NECommand.Edit_Advanced_EscapeClearsSelections: PreExecute_Edit_Advanced_EscapeClearsSelections(); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Ordered_Match_IgnoreCase: PreExecute_Text_Select_Repeats_BetweenFiles_Ordered_MatchMismatch_IgnoreMatchCase(false); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Ordered_Match_MatchCase: PreExecute_Text_Select_Repeats_BetweenFiles_Ordered_MatchMismatch_IgnoreMatchCase(true); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Ordered_Mismatch_IgnoreCase: PreExecute_Text_Select_Repeats_BetweenFiles_Ordered_MatchMismatch_IgnoreMatchCase(false); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Ordered_Mismatch_MatchCase: PreExecute_Text_Select_Repeats_BetweenFiles_Ordered_MatchMismatch_IgnoreMatchCase(true); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Unordered_Match_IgnoreCase: PreExecute_Text_Select_Repeats_BetweenFiles_Unordered_MatchMismatch_IgnoreMatchCase(false); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Unordered_Match_MatchCase: PreExecute_Text_Select_Repeats_BetweenFiles_Unordered_MatchMismatch_IgnoreMatchCase(true); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Unordered_Mismatch_IgnoreCase: PreExecute_Text_Select_Repeats_BetweenFiles_Unordered_MatchMismatch_IgnoreMatchCase(false); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Unordered_Mismatch_MatchCase: PreExecute_Text_Select_Repeats_BetweenFiles_Unordered_MatchMismatch_IgnoreMatchCase(true); break;
				case NECommand.Files_Select_BySourceControlStatus: PreExecute_Files_SelectGet_BySourceControlStatus(); break;
				case NECommand.Files_Get_SourceControlStatus: PreExecute_Files_SelectGet_BySourceControlStatus(); break;
				case NECommand.Diff_Select_LeftFile: PreExecute_Diff_Select_LeftRightBothFiles(true); break;
				case NECommand.Diff_Select_RightFile: PreExecute_Diff_Select_LeftRightBothFiles(false); break;
				case NECommand.Diff_Select_BothFiles: PreExecute_Diff_Select_LeftRightBothFiles(null); break;
				case NECommand.Diff_Diff: PreExecute_Diff_Diff(); break;
				case NECommand.Window_New_New: PreExecute_Window_New_New(); break;
				case NECommand.Window_New_FromSelections_All: PreExecute_Window_New_FromSelections_All(); break;
				case NECommand.Window_New_FromSelections_Files: PreExecute_Window_New_FromSelections_Files(); break;
				case NECommand.Window_New_FromSelections_Selections: PreExecute_Window_New_FromSelections_Selections(); break;
				case NECommand.Window_New_SummarizeSelections_Files_IgnoreCase: PreExecute_Window_New_SummarizeSelections_AllSelectionsEachFile_IgnoreMatchCase(false, true); break;
				case NECommand.Window_New_SummarizeSelections_Files_MatchCase: PreExecute_Window_New_SummarizeSelections_AllSelectionsEachFile_IgnoreMatchCase(true, true); break;
				case NECommand.Window_New_SummarizeSelections_Selections_IgnoreCase: PreExecute_Window_New_SummarizeSelections_AllSelectionsEachFile_IgnoreMatchCase(false, false); break;
				case NECommand.Window_New_SummarizeSelections_Selections_MatchCase: PreExecute_Window_New_SummarizeSelections_AllSelectionsEachFile_IgnoreMatchCase(true, false); break;
				case NECommand.Window_New_FromClipboard_All: PreExecute_Window_New_FromClipboard_All(); break;
				case NECommand.Window_New_FromClipboard_Files: PreExecute_Window_New_FromClipboard_Files(); break;
				case NECommand.Window_New_FromClipboard_Selections: PreExecute_Window_New_FromClipboard_Selections(); break;
				case NECommand.Window_New_FromFiles_Active: PreExecute_Window_New_FromFiles_Active(); break;
				case NECommand.Window_New_FromFiles_CopiedCut: PreExecute_Window_New_FromFiles_CopiedCut(); break;
				case NECommand.Window_Full: PreExecute_Window_Full(); break;
				case NECommand.Window_Grid: PreExecute_Window_Grid(); break;
				case NECommand.Window_CustomGrid: PreExecute_Window_CustomGrid(); break;
				case NECommand.Window_ActiveFirst: PreExecute_Window_ActiveFirst(); break;
				case NECommand.Window_Font_Size: PreExecute_Window_Font_Size(); break;
				case NECommand.Window_Font_ShowSpecial: PreExecute_Window_Font_ShowSpecial(); break;
				case NECommand.Help_Tutorial: PreExecute_Help_Tutorial(); break;
				case NECommand.Help_Update: PreExecute_Help_Update(); break;
				case NECommand.Help_Advanced_Shell_Integrate: PreExecute_Help_Advanced_Shell_Integrate(); break;
				case NECommand.Help_Advanced_Shell_Unintegrate: PreExecute_Help_Advanced_Shell_Unintegrate(); break;
				case NECommand.Help_Advanced_CopyCommandLine: PreExecute_Help_Advanced_CopyCommandLine(); break;
				case NECommand.Help_Advanced_Extract: PreExecute_Help_Advanced_Extract(); break;
				case NECommand.Help_Advanced_RunGC: PreExecute_Help_Advanced_RunGC(); break;
				case NECommand.Help_About: PreExecute_Help_About(); break;
			}

			return false;
		}
		#endregion

		#region Execute
		public void Execute()
		{
			switch (state.Command)
			{
				case NECommand.Internal_Key: Execute_Internal_Key(); break;
				case NECommand.Internal_Text: Execute_Internal_Text(); break;
				case NECommand.Internal_SetBinaryValue: Execute_Internal_SetBinaryValue(); break;
				case NECommand.File_New_FromSelections_All: Execute_File_New_FromSelections_AllFilesSelections(); break;
				case NECommand.File_New_FromSelections_Files: Execute_File_New_FromSelections_AllFilesSelections(); break;
				case NECommand.File_New_FromSelections_Selections: Execute_File_New_FromSelections_AllFilesSelections(); break;
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
				case NECommand.File_Close_WithSelections: Execute_File_Close_FilesWithWithoutSelections(true); break;
				case NECommand.File_Close_WithoutSelections: Execute_File_Close_FilesWithWithoutSelections(false); break;
				case NECommand.File_Close_Modified: Execute_File_Close_ModifiedUnmodifiedFiles(true); break;
				case NECommand.File_Close_Unmodified: Execute_File_Close_ModifiedUnmodifiedFiles(false); break;
				case NECommand.Edit_Select_All: Execute_Edit_Select_All(); break;
				case NECommand.Edit_Select_Nothing: Execute_Edit_Select_Nothing(); break;
				case NECommand.Edit_Select_Join: Execute_Edit_Select_Join(); break;
				case NECommand.Edit_Select_Invert: Execute_Edit_Select_Invert(); break;
				case NECommand.Edit_Select_Limit: Execute_Edit_Select_Limit(); break;
				case NECommand.Edit_Select_Lines: Execute_Edit_Select_Lines(); break;
				case NECommand.Edit_Select_WholeLines: Execute_Edit_Select_WholeLines(); break;
				case NECommand.Edit_Select_Empty: Execute_Edit_Select_EmptyNonEmpty(true); break;
				case NECommand.Edit_Select_NonEmpty: Execute_Edit_Select_EmptyNonEmpty(false); break;
				case NECommand.Edit_Select_Overlap_DeOverlap: Execute_Edit_Select_Overlap_DeOverlap(); break;
				case NECommand.Edit_Select_Overlap_AllowOverlappingSelections: Execute_Edit_Select_Overlap_AllowOverlappingSelections(); break;
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
				case NECommand.Edit_Paste_Paste: Execute_Edit_Paste_PasteRotatePaste(state.ShiftDown, false); break;
				case NECommand.Edit_Paste_RotatePaste: Execute_Edit_Paste_PasteRotatePaste(true, true); break;
				case NECommand.Edit_Undo_Text: Execute_Edit_Undo_Text(); break;
				case NECommand.Edit_Undo_Step: Execute_Edit_Undo_Step(); break;
				case NECommand.Edit_Redo_Text: Execute_Edit_Redo_Text(); break;
				case NECommand.Edit_Redo_Step: Execute_Edit_Redo_Step(); break;
				case NECommand.Edit_Repeat: Execute_Edit_Repeat(); break;
				case NECommand.Edit_Rotate: Execute_Edit_Rotate(); break;
				case NECommand.Edit_Expression_Expression: Execute_Edit_Expression_Expression(); break;
				case NECommand.Edit_Expression_EvaluateSelected: Execute_Edit_Expression_EvaluateSelected(); break;
				case NECommand.Edit_ModifyRegions: Execute_Edit_ModifyRegions_Various_Various_Region(state.Configuration as Configuration_Edit_ModifyRegions); break;
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
				case NECommand.Text_Select_Repeats_BetweenFiles_Ordered_Match_IgnoreCase: Execute_Text_Select_Repeats_BetweenFiles_MatchMismatch_IgnoreMatchCase(true); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Ordered_Match_MatchCase: Execute_Text_Select_Repeats_BetweenFiles_MatchMismatch_IgnoreMatchCase(true); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Ordered_Mismatch_IgnoreCase: Execute_Text_Select_Repeats_BetweenFiles_MatchMismatch_IgnoreMatchCase(false); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Ordered_Mismatch_MatchCase: Execute_Text_Select_Repeats_BetweenFiles_MatchMismatch_IgnoreMatchCase(false); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Unordered_Match_IgnoreCase: Execute_Text_Select_Repeats_BetweenFiles_CommonNonCommon_IgnoreMatchCase(true); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Unordered_Match_MatchCase: Execute_Text_Select_Repeats_BetweenFiles_CommonNonCommon_IgnoreMatchCase(true); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Unordered_Mismatch_IgnoreCase: Execute_Text_Select_Repeats_BetweenFiles_CommonNonCommon_IgnoreMatchCase(false); break;
				case NECommand.Text_Select_Repeats_BetweenFiles_Unordered_Mismatch_MatchCase: Execute_Text_Select_Repeats_BetweenFiles_CommonNonCommon_IgnoreMatchCase(false); break;
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
				case NECommand.Content_Navigate_Up: Execute_Content_Navigate_Various(ParserNode.ParserNavigationDirectionEnum.Up, state.ShiftDown); break;
				case NECommand.Content_Navigate_Down: Execute_Content_Navigate_Various(ParserNode.ParserNavigationDirectionEnum.Down, state.ShiftDown); break;
				case NECommand.Content_Navigate_Left: Execute_Content_Navigate_Various(ParserNode.ParserNavigationDirectionEnum.Left, state.ShiftDown); break;
				case NECommand.Content_Navigate_Right: Execute_Content_Navigate_Various(ParserNode.ParserNavigationDirectionEnum.Right, state.ShiftDown); break;
				case NECommand.Content_Navigate_Home: Execute_Content_Navigate_Various(ParserNode.ParserNavigationDirectionEnum.Home, state.ShiftDown); break;
				case NECommand.Content_Navigate_End: Execute_Content_Navigate_Various(ParserNode.ParserNavigationDirectionEnum.End, state.ShiftDown); break;
				case NECommand.Content_Navigate_Pgup: Execute_Content_Navigate_Various(ParserNode.ParserNavigationDirectionEnum.PgUp, state.ShiftDown); break;
				case NECommand.Content_Navigate_Pgdn: Execute_Content_Navigate_Various(ParserNode.ParserNavigationDirectionEnum.PgDn, state.ShiftDown); break;
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
				case NECommand.Position_Goto_Lines: Execute_Position_Goto_Various(GotoType.Line, state.ShiftDown); break;
				case NECommand.Position_Goto_Columns: Execute_Position_Goto_Various(GotoType.Column, state.ShiftDown); break;
				case NECommand.Position_Goto_Indexes: Execute_Position_Goto_Various(GotoType.Index, state.ShiftDown); break;
				case NECommand.Position_Goto_Positions: Execute_Position_Goto_Various(GotoType.Position, state.ShiftDown); break;
				case NECommand.Position_Copy_Lines: Execute_Position_Copy_Various(GotoType.Line, false); break;
				case NECommand.Position_Copy_Columns: Execute_Position_Copy_Various(GotoType.Column, !state.ShiftDown); break;
				case NECommand.Position_Copy_Indexes: Execute_Position_Copy_Various(GotoType.Index, !state.ShiftDown); break;
				case NECommand.Position_Copy_Positions: Execute_Position_Copy_Various(GotoType.Position, false); break;
				case NECommand.Diff_Select_Matches: Execute_Diff_Select_MatchesDiffs(true); break;
				case NECommand.Diff_Select_Diffs: Execute_Diff_Select_MatchesDiffs(false); break;
				case NECommand.Diff_Break: Execute_Diff_Break(); break;
				case NECommand.Diff_SourceControl: Execute_Diff_SourceControl(); break;
				case NECommand.Diff_IgnoreWhitespace: Execute_Diff_IgnoreWhitespace(state.MultiStatus); break;
				case NECommand.Diff_IgnoreCase: Execute_Diff_IgnoreCase(state.MultiStatus); break;
				case NECommand.Diff_IgnoreNumbers: Execute_Diff_IgnoreNumbers(state.MultiStatus); break;
				case NECommand.Diff_IgnoreLineEndings: Execute_Diff_IgnoreLineEndings(state.MultiStatus); break;
				case NECommand.Diff_IgnoreCharacters: Execute_Diff_IgnoreCharacters(); break;
				case NECommand.Diff_Reset: Execute_Diff_Reset(); break;
				case NECommand.Diff_Next: Execute_Diff_NextPrevious(true, state.ShiftDown); break;
				case NECommand.Diff_Previous: Execute_Diff_NextPrevious(false, state.ShiftDown); break;
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
				case NECommand.Window_ViewBinary: Execute_Window_ViewBinary(); break;
				case NECommand.Window_BinaryCodePages: Execute_Window_BinaryCodePages(); break;
			}
		}
		#endregion

		#region PostExecute
		public void PostExecute()
		{
		}
		#endregion

		#region Watcher
		public bool watcherFileModified = false;
		FileSystemWatcher watcher = null;
		DateTime fileLastWrite { get; set; }

		void SetAutoRefresh(bool? value = null)
		{
			watcher?.Dispose();
			watcher = null;

			if (value.HasValue)
				AutoRefresh = value.Value;

			if ((NEWindow == null) || (!AutoRefresh) || (!File.Exists(FileName)))
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
				try { NEWindow?.neWindowUI?.SendActivateIfActive(); } catch { }
			};
			watcher.EnableRaisingEvents = true;
		}
		#endregion

		void SetModifiedFlag(bool? newValue = null)
		{
			if (newValue.HasValue)
			{
				if (newValue == false)
					modifiedChecksum.SetValue(Text.GetString());
				else
					modifiedChecksum.Invalidate(); // Nothing will match, file will be perpetually modified
			}
			IsModified = !modifiedChecksum.Match(Text.GetString());
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
				StartRow = (lineMin + lineMax - NEWindow.DisplayRows) / 2;
				if (centerHorizontally)
					StartColumn = (Text.GetColumnFromIndex(lineMin, indexMin) + Text.GetColumnFromIndex(lineMax, indexMax) - NEWindow.DisplayColumns) / 2;
				else
					StartColumn = 0;
			}

			var line = Text.GetPositionLine(range.Cursor);
			var index = Text.GetPositionIndex(range.Cursor, line);
			var x = Text.GetColumnFromIndex(line, index);
			StartRow = Math.Min(line, Math.Max(line - (NEWindow?.DisplayRows ?? 1) + 1, StartRow));
			StartColumn = Math.Min(x, Math.Max(x - (NEWindow?.DisplayColumns ?? 1) + 1, StartColumn));
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

		List<T> GetExpressionResults<T>(string expression, int? count = null) => state.GetExpression(expression).EvaluateList<T>(GetVariables(), count);

		List<NERange> GetEnclosingRegions(int useRegion, bool useAllRegions = false, bool mustBeInRegion = true)
		{
			var useRegions = GetRegions(useRegion);
			var regions = new List<NERange>();
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
			AddNewNEFile(new NEFile(displayName: name, bytes: Coder.StringToBytes(table.ToString("\r\n", contentType), Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, contentType: contentType, modified: false));
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
			{
				CodePage = Coder.CodePage.UTF8;
				HasBOM = true;
			}
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
					File.WriteAllBytes(fileName, FileSaver.Encrypt(FileSaver.Compress(Text.GetBytes(CodePage, HasBOM), Compressed), AESKey));
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

		void OpenFile(string fileName, string displayName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, ParserType contentType = ParserType.None, bool? modified = null)
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

			CodePage = Coder.ResolveCodePage(codePage, bytes);
			HasBOM = Coder.HasBOM(bytes, CodePage);

			var data = Coder.BytesToString(bytes, CodePage, true);
			Replace(new List<NERange> { NERange.FromIndex(0, Text.Length) }, new List<string> { data });

			if (File.Exists(FileName))
				fileLastWrite = new FileInfo(FileName).LastWriteTime;

			// If encoding can't exactly express bytes mark as modified (only for < 50 MB)
			if ((!isModified) && ((bytes.Length >> 20) < 50))
				isModified = !Coder.CanExactlyEncode(bytes, CodePage);

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
			Selections = new List<NERange> { new NERange(pos) };
		}

		string savedBitmapText;
		System.Drawing.Bitmap savedBitmap;
		readonly List<ShutdownData> shutdownDatas = new List<ShutdownData>();

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

		public void AddShutdownData(ShutdownData shutdownData)
		{
			if (shutdownData != null)
				shutdownDatas.Add(shutdownData);
		}

		public void Close()
		{
			DiffTarget = null;
			shutdownDatas.ForEach(shutdownData => shutdownData.OnShutdown());
			shutdownDatas.Clear();
			ClearNEFiles();
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

		public void ViewSetDisplaySize(int columns, int rows) => NEWindow?.SetDisplaySize(columns, rows);

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
			lock (state)
			{
				if ((!state.SavedAnswers[name].HasFlag(MessageOptions.All)) && (!state.SavedAnswers[name].HasFlag(MessageOptions.Cancel)))
					NEWindow.ShowFile(this, () => state.SavedAnswers[name] = NEWindow.neWindowUI.RunDialog_ShowMessage("Confirm", text, MessageOptions.YesNoAllCancel, defaultAccept, MessageOptions.Cancel));
				if (state.SavedAnswers[name] == MessageOptions.Cancel)
					throw new OperationCanceledException();
				return state.SavedAnswers[name].HasFlag(MessageOptions.Yes);
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

			// Use serial to make sure the diff always happens the same way (neFile1 vs neFile2 is slightly different from neFile2 vs neFile1)
			var neFile1 = serial < DiffTarget.serial ? this : DiffTarget;
			var neFile2 = neFile1 == this ? DiffTarget : this;

			lock (neFile1)
				NEText.CalculateDiff(neFile1.Text, neFile2.Text, DiffIgnoreWhitespace, DiffIgnoreCase, DiffIgnoreNumbers, DiffIgnoreLineEndings, DiffIgnoreCharacters);
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

		public bool CheckModified(MessageOptions defaultAccept)
		{
			if (!IsModified)
				return true;
			return QueryUser(nameof(Execute_File_Open_ReopenWithEncoding), "You have unsaved changes. Are you sure you want to reload?", defaultAccept);
		}
	}
}
