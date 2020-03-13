using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using NeoEdit.Program.Dialogs;
using NeoEdit.Program.Expressions;
using NeoEdit.Program.Parsing;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program
{
	public partial class TextEditor
	{
		static ThreadSafeRandom random = new ThreadSafeRandom();
		const int tabStop = 4;

		public TabsWindow tabsWindow;
		ExecuteState state;

		public TextEditor(string fileName = null, string displayName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, ParserType contentType = ParserType.None, bool? modified = null, int? line = null, int? column = null, int? index = null, ShutdownData shutdownData = null)
		{
			BeginTransaction();

			Text = new NEText("");
			Selections = new List<Range>();
			for (var region = 1; region <= 9; ++region)
				SetRegions(region, new List<Range>());
			newUndoRedo = new UndoRedo();

			fileName = fileName?.Trim('"');
			this.shutdownData = shutdownData;

			AutoRefresh = KeepSelections = HighlightSyntax = true;
			JumpBy = JumpByType.Words;

			OpenFile(fileName, displayName, bytes, codePage, contentType, modified);
			Goto(line, column, index);
		}

		public string TabLabel => $"{DisplayName ?? (string.IsNullOrEmpty(FileName) ? "[Untitled]" : Path.GetFileName(FileName))}{(IsModified ? "*" : "")}{(IsDiff ? $" (Diff{(DiffEncodingMismatch ? " - Encoding mismatch" : "")})" : "")}";

		CacheValue modifiedChecksum = new CacheValue();
		CacheValue previousData = new CacheValue();
		ParserType previousType;
		ParserNode previousRoot;

		public void ReplaceOneWithMany(IReadOnlyList<string> strs, bool? addNewLines)
		{
			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			var ending = addNewLines ?? strs.Any(str => !str.EndsWith(TextView.DefaultEnding)) ? TextView.DefaultEnding : "";
			if (ending.Length != 0)
				strs = strs.Select(str => str + ending).ToList();
			var position = Selections.Single().Start;
			ReplaceSelections(string.Join("", strs));

			var sels = new List<Range>();
			foreach (var str in strs)
			{
				sels.Add(Range.FromIndex(position, str.Length - ending.Length));
				position += str.Length;
			}
			Selections = sels;
		}

		public void ReplaceSelections(string str, bool highlight = true, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false) => ReplaceSelections(Enumerable.Repeat(str, Selections.Count).ToList(), highlight, replaceType, tryJoinUndo);

		public void ReplaceSelections(IReadOnlyList<string> strs, bool highlight = true, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false)
		{
			Replace(Selections, strs, replaceType, tryJoinUndo);

			if (highlight)
				Selections = Selections.AsParallel().AsOrdered().Select((range, index) => new Range(range.End, range.End - (strs == null ? 0 : strs[index].Length))).ToList();
			else
				Selections = Selections.AsParallel().AsOrdered().Select(range => new Range(range.End)).ToList();
		}

		public void Replace(IReadOnlyList<Range> ranges, IReadOnlyList<string> strs, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false)
		{
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
				case ReplaceType.Undo: UndoRedo.AddUndone(ref newUndoRedo, textCanvasUndoRedo); break;
				case ReplaceType.Redo: UndoRedo.AddRedone(ref newUndoRedo, textCanvasUndoRedo); break;
				case ReplaceType.Normal: UndoRedo.AddUndo(ref newUndoRedo, textCanvasUndoRedo, IsModified); break;
			}

			Text = Text.Replace(ranges, strs);
			SetModifiedFlag();

			var translateMap = GetTranslateMap(ranges, strs, new List<IReadOnlyList<Range>> { Selections }.Concat(Enumerable.Range(1, 9).Select(region => GetRegions(region))).ToList());
			Selections = Translate(Selections, translateMap);
			for (var region = 1; region <= 9; ++region)
				SetRegions(region, Translate(GetRegions(region), translateMap));
		}

		public int NumLines => TextView.NumLines;

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

		#region Columns
		public int GetPositionLine(int position) => TextView.GetPositionLine(position);
		public int GetPositionIndex(int position, int line) => TextView.GetPositionIndex(position, line);
		public int GetLineLength(int line) => TextView.GetLineLength(line);
		public int GetPosition(int line, int index, bool allowJustPastEnd = false) => TextView.GetPosition(line, index, allowJustPastEnd);

		public int GetIndexFromColumn(int line, int findColumn, bool returnMaxOnFail = false)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();
			if (findColumn < 0)
				throw new IndexOutOfRangeException();

			var column = 0;
			var position = TextView.GetPosition(line, 0);
			var end = position + GetLineLength(line);
			while (column < findColumn)
			{
				var find = Text.IndexOf('\t', position, end - position);
				if (find == position)
				{
					column = (column / tabStop + 1) * tabStop;
					++position;
					continue;
				}

				if (find == -1)
					find = findColumn - column;
				else
					find = Math.Min(find - position, findColumn - column);

				column += find;
				position += find;
			}
			if (position > end + 1)
			{
				if (returnMaxOnFail)
					return GetLineLength(line) + 1;
				throw new IndexOutOfRangeException();
			}
			return position - TextView.GetPosition(line, 0);
		}

		public int GetColumnFromIndex(int line, int findIndex)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();
			if ((findIndex < 0) || (findIndex > GetLineLength(line) + 1))
				throw new IndexOutOfRangeException();

			var column = 0;
			var position = TextView.GetPosition(line, 0);
			var findPosition = findIndex + position;
			var end = position + GetLineLength(line);
			while (position < findPosition)
			{
				var find = Text.IndexOf('\t', position, end - position);
				if (find == position)
				{
					column = (column / tabStop + 1) * tabStop;
					++position;
					continue;
				}

				if (find == -1)
					find = findPosition - position;
				else
					find = Math.Min(find, findPosition) - position;

				column += find;
				position += find;
			}
			return column;
		}

		public int GetLineColumnsLength(int line)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();

			var index = TextView.GetPosition(line, 0);
			var len = TextView.GetLineLength(line);
			var columns = 0;
			while (len > 0)
			{
				var find = Text.IndexOf('\t', index, len);
				if (find == index)
				{
					columns = (columns / tabStop + 1) * tabStop;
					++index;
					--len;
					continue;
				}

				if (find == -1)
					find = len;
				else
					find -= index;
				columns += find;
				index += find;
				len -= find;
			}

			return columns;
		}

		public string GetLineColumns(int line, int startColumn, int endColumn)
		{
			if ((line < 0) || (line >= TextView.NumLines))
				throw new IndexOutOfRangeException();

			var column = 0;
			var index = TextView.GetPosition(line, 0);
			var endIndex = index + TextView.GetLineLength(line);
			var sb = new StringBuilder();
			while ((column < endColumn) && (index < endIndex))
			{
				var skipColumns = Math.Max(0, startColumn - column);
				var takeColumns = endColumn - column;

				var tabIndex = Text.IndexOf('\t', index, Math.Min(endIndex - index, takeColumns));
				if (tabIndex == index)
				{
					var repeatCount = (column / tabStop + 1) * tabStop - column;
					var useColumns = Math.Min(Math.Max(0, repeatCount - skipColumns), takeColumns);
					sb.Append(' ', useColumns);
					column += repeatCount;
					++index;
					continue;
				}

				if (tabIndex == -1)
					tabIndex = endIndex;

				if (skipColumns > 0)
				{
					var useColumns = Math.Min(skipColumns, tabIndex - index);
					index += useColumns;
					column += useColumns;
				}

				{
					var useColumns = Math.Min(tabIndex - index, takeColumns);
					sb.Append(Text.GetString(index, useColumns));
					column += useColumns;
					index += useColumns;
				}
			}

			return sb.ToString();
		}
		#endregion

		#region PreExecute
		public void PreExecute()
		{
			switch (state.Command)
			{
				case NECommand.Internal_Key: PreExecute_Internal_Key(); break;
				case NECommand.Edit_Paste_Paste: PreExecute_Edit_Paste_Paste(); break;
				case NECommand.Edit_Paste_RotatePaste: PreExecute_Edit_Paste_Paste(); break;
				case NECommand.Select_RepeatsCaseSensitive_Tabs_Match: PreExecute_Select_Repeats_Tabs_MatchMismatch(true); break;
				case NECommand.Select_RepeatsCaseSensitive_Tabs_Mismatch: PreExecute_Select_Repeats_Tabs_MatchMismatch(true); break;
				case NECommand.Select_RepeatsCaseInsensitive_Tabs_Match: PreExecute_Select_Repeats_Tabs_MatchMismatch(false); break;
				case NECommand.Select_RepeatsCaseInsensitive_Tabs_Mismatch: PreExecute_Select_Repeats_Tabs_MatchMismatch(false); break;
				case NECommand.Select_RepeatsCaseSensitive_Tabs_Common: PreExecute_Select_Repeats_Tabs_CommonNonCommon(true); break;
				case NECommand.Select_RepeatsCaseSensitive_Tabs_NonCommon: PreExecute_Select_Repeats_Tabs_CommonNonCommon(true); break;
				case NECommand.Select_RepeatsCaseInsensitive_Tabs_Common: PreExecute_Select_Repeats_Tabs_CommonNonCommon(false); break;
				case NECommand.Select_RepeatsCaseInsensitive_Tabs_NonCommon: PreExecute_Select_Repeats_Tabs_CommonNonCommon(false); break;
				case NECommand.Select_Selection_ToggleAnchor: PreExecute_Select_Selection_ToggleAnchor(); break;
			}
		}

		void PreExecute_Internal_Key()
		{
			switch ((Key)state.ConfigureExecuteData)
			{
				case Key.Back:
				case Key.Delete:
				case Key.Left:
				case Key.Right:
					state.PreExecuteData = (state.PreExecuteData as bool? ?? false) || (Selections.Any(range => range.HasSelection));
					break;
			}
		}
		#endregion

		#region ConfigureExecute
		public void ConfigureExecute()
		{
			switch (state.Command)
			{
				case NECommand.File_Save_SaveAsByExpression: ConfigureExecute_File_SaveCopy_SaveCopyByExpression(); break;
				case NECommand.File_Copy_CopyToByExpression: ConfigureExecute_File_SaveCopy_SaveCopyByExpression(); break;
				case NECommand.File_Operations_RenameByExpression: ConfigureExecute_File_Operations_RenameByExpression(); break;
				case NECommand.File_Operations_SetDisplayName: ConfigureExecute_File_SaveCopy_SaveCopyByExpression(); break;
				case NECommand.File_Encoding_Encoding: ConfigureExecute_File_Encoding_Encoding(); break;
				case NECommand.File_Encoding_ReopenWithEncoding: ConfigureExecute_File_Encoding_ReopenWithEncoding(); break;
				case NECommand.File_Encoding_LineEndings: ConfigureExecute_File_Encoding_LineEndings(); break;
				case NECommand.File_Encrypt: ConfigureExecute_File_Encrypt(); break;
				case NECommand.Edit_Find_Find: ConfigureExecute_Edit_Find_Find(); break;
				case NECommand.Edit_Find_RegexReplace: ConfigureExecute_Edit_Find_RegexReplace(); break;
				case NECommand.Edit_Rotate: ConfigureExecute_Edit_Rotate(); break;
				case NECommand.Edit_Repeat: ConfigureExecute_Edit_Repeat(); break;
				case NECommand.Edit_Data_Hash: ConfigureExecute_Edit_Data_Hash(); break;
				case NECommand.Edit_Data_Compress: ConfigureExecute_Edit_Data_Compress(); break;
				case NECommand.Edit_Data_Decompress: ConfigureExecute_Edit_Data_Decompress(); break;
				case NECommand.Edit_Data_Encrypt: ConfigureExecute_Edit_Data_Encrypt(); break;
				case NECommand.Edit_Data_Decrypt: ConfigureExecute_Edit_Data_Decrypt(); break;
				case NECommand.Edit_Data_Sign: ConfigureExecute_Edit_Data_Sign(); break;
				case NECommand.Edit_Sort: ConfigureExecute_Edit_Sort(); break;
				case NECommand.Edit_Convert: ConfigureExecute_Edit_Convert(); break;
				case NECommand.Edit_ModifyRegions: ConfigureExecute_Edit_ModifyRegions(); break;
				//TODO
				//case NECommand.Diff_IgnoreCharacters: Command_Diff_IgnoreCharacters_Dialog(); break;
				//case NECommand.Diff_Fix_Whitespace: Command_Diff_Fix_Whitespace_Dialog(); break;
				case NECommand.Files_Name_MakeAbsolute: ConfigureExecute_Files_Name_MakeAbsolute(); break;
				case NECommand.Files_Name_MakeRelative: ConfigureExecute_Files_Name_MakeRelative(); break;
				case NECommand.Files_Name_GetUnique: ConfigureExecute_Files_Name_GetUnique(); break;
				case NECommand.Files_Set_Size: ConfigureExecute_Files_Set_Size(); break;
				case NECommand.Files_Set_Time_Write: ConfigureExecute_Files_Set_Time(); break;
				case NECommand.Files_Set_Time_Access: ConfigureExecute_Files_Set_Time(); break;
				case NECommand.Files_Set_Time_Create: ConfigureExecute_Files_Set_Time(); break;
				case NECommand.Files_Set_Time_All: ConfigureExecute_Files_Set_Time(); break;
				case NECommand.Files_Set_Attributes: ConfigureExecute_Files_Set_Attributes(); break;
				case NECommand.Files_Find: ConfigureExecute_Files_Find(); break;
				case NECommand.Files_Insert: ConfigureExecute_Files_Insert(); break;
				case NECommand.Files_Create_FromExpressions: ConfigureExecute_Files_Create_FromExpressions(); break;
				case NECommand.Files_Select_ByVersionControlStatus: ConfigureExecute_Files_Select_ByVersionControlStatus(); break;
				case NECommand.Files_Hash: ConfigureExecute_Files_Hash(); break;
				case NECommand.Files_Sign: ConfigureExecute_Files_Sign(); break;
				case NECommand.Files_Operations_Copy: ConfigureExecute_Files_Operations_CopyMove(false); break;
				case NECommand.Files_Operations_Move: ConfigureExecute_Files_Operations_CopyMove(true); break;
				case NECommand.Files_Operations_Encoding: ConfigureExecute_Files_Operations_Encoding(); break;
				case NECommand.Files_Operations_SplitFile: ConfigureExecute_Files_Operations_SplitFile(); break;
				case NECommand.Files_Operations_CombineFiles: ConfigureExecute_Files_Operations_CombineFiles(); break;
				case NECommand.Edit_Expression_Expression: ConfigureExecute_Edit_Expression_Expression(); break;
				case NECommand.Text_Select_Trim: ConfigureExecute_Text_Select_Trim(); break;
				case NECommand.Text_Select_ByWidth: ConfigureExecute_Text_Select_ByWidth(); break;
				case NECommand.Text_Select_WholeWord: ConfigureExecute_Text_Select_WholeBoundedWord(true); break;
				case NECommand.Text_Select_BoundedWord: ConfigureExecute_Text_Select_WholeBoundedWord(false); break;
				case NECommand.Text_Width: ConfigureExecute_Text_Width(); break;
				case NECommand.Text_Trim: ConfigureExecute_Text_Trim(); break;
				case NECommand.Text_Unicode: ConfigureExecute_Text_Unicode(); break;
				case NECommand.Text_RandomText: ConfigureExecute_Text_RandomText(); break;
				case NECommand.Text_ReverseRegEx: ConfigureExecute_Text_ReverseRegEx(); break;
				case NECommand.Text_FirstDistinct: ConfigureExecute_Text_FirstDistinct(); break;
				case NECommand.Numeric_ConvertBase: ConfigureExecute_Numeric_ConvertBase(); break;
				case NECommand.Numeric_Series_Linear: ConfigureExecute_Numeric_Series_LinearGeometric(true); break;
				case NECommand.Numeric_Series_Geometric: ConfigureExecute_Numeric_Series_LinearGeometric(false); break;
				case NECommand.Numeric_Scale: ConfigureExecute_Numeric_Scale(); break;
				case NECommand.Numeric_Floor: ConfigureExecute_Numeric_Floor(); break;
				case NECommand.Numeric_Ceiling: ConfigureExecute_Numeric_Ceiling(); break;
				case NECommand.Numeric_Round: ConfigureExecute_Numeric_Round(); break;
				case NECommand.Numeric_Limit: ConfigureExecute_Numeric_Limit(); break;
				case NECommand.Numeric_Cycle: ConfigureExecute_Numeric_Cycle(); break;
				case NECommand.Numeric_RandomNumber: ConfigureExecute_Numeric_RandomNumber(); break;
				case NECommand.Numeric_CombinationsPermutations: ConfigureExecute_Numeric_CombinationsPermutations(); break;
				case NECommand.Numeric_MinMaxValues: ConfigureExecute_Numeric_MinMaxValues(); break;
				case NECommand.DateTime_Format: ConfigureExecute_DateTime_Format(); break;
				case NECommand.DateTime_ToTimeZone: ConfigureExecute_DateTime_ToTimeZone(); break;
				case NECommand.Image_GrabColor: ConfigureExecute_Image_GrabColor(); break;
				case NECommand.Image_GrabImage: ConfigureExecute_Image_GrabImage(); break;
				case NECommand.Image_AdjustColor: ConfigureExecute_Image_AdjustColor(); break;
				case NECommand.Image_AddColor: ConfigureExecute_Image_AddOverlayColor(true); break;
				case NECommand.Image_OverlayColor: ConfigureExecute_Image_AddOverlayColor(false); break;
				case NECommand.Image_Size: ConfigureExecute_Image_Size(); break;
				case NECommand.Image_Crop: ConfigureExecute_Image_Crop(); break;
				case NECommand.Image_Rotate: ConfigureExecute_Image_Rotate(); break;
				case NECommand.Image_GIF_Animate: ConfigureExecute_Image_GIF_Animate(); break;
				case NECommand.Image_GIF_Split: ConfigureExecute_Image_GIF_Split(); break;
				case NECommand.Table_Convert: ConfigureExecute_Table_Convert(); break;
				case NECommand.Table_TextToTable: ConfigureExecute_Table_TextToTable(); break;
				case NECommand.Table_EditTable: ConfigureExecute_Table_EditTable(); break;
				case NECommand.Table_AddColumn: ConfigureExecute_Table_AddColumn(); break;
				case NECommand.Table_Select_RowsByExpression: ConfigureExecute_Table_Select_RowsByExpression(); break;
				case NECommand.Table_Join: ConfigureExecute_Table_Join(); break;
				case NECommand.Table_Database_GenerateInserts: ConfigureExecute_Table_Database_GenerateInserts(); break;
				case NECommand.Table_Database_GenerateUpdates: ConfigureExecute_Table_Database_GenerateUpdates(); break;
				case NECommand.Table_Database_GenerateDeletes: ConfigureExecute_Table_Database_GenerateDeletes(); break;
				case NECommand.Position_Goto_Lines: ConfigureExecute_Position_Goto(GotoType.Line); break;
				case NECommand.Position_Goto_Columns: ConfigureExecute_Position_Goto(GotoType.Column); break;
				case NECommand.Position_Goto_Indexes: ConfigureExecute_Position_Goto(GotoType.Index); break;
				case NECommand.Position_Goto_Positions: ConfigureExecute_Position_Goto(GotoType.Position); break;
				case NECommand.Content_Ancestor: ConfigureExecute_Content_Ancestor(); break;
				case NECommand.Content_Attributes: ConfigureExecute_Content_Attributes(); break;
				case NECommand.Content_WithAttribute: ConfigureExecute_Content_WithAttribute(); break;
				case NECommand.Content_Children_WithAttribute: ConfigureExecute_Content_Children_WithAttribute(); break;
				case NECommand.Content_Descendants_WithAttribute: ConfigureExecute_Content_Descendants_WithAttribute(); break;
				case NECommand.Network_AbsoluteURL: ConfigureExecute_Network_AbsoluteURL(); break;
				case NECommand.Network_FetchFile: ConfigureExecute_Network_FetchFile(); break;
				case NECommand.Network_FetchStream: ConfigureExecute_Network_FetchStream(); break;
				case NECommand.Network_FetchPlaylist: ConfigureExecute_Network_FetchPlaylist(); break;
				case NECommand.Network_Ping: ConfigureExecute_Network_Ping(); break;
				case NECommand.Network_ScanPorts: ConfigureExecute_Network_ScanPorts(); break;
				case NECommand.Network_WCF_GetConfig: ConfigureExecute_Network_WCF_GetConfig(); break;
				case NECommand.Network_WCF_InterceptCalls: ConfigureExecute_Network_WCF_InterceptCalls(); break;
				case NECommand.Database_Connect: ConfigureExecute_Database_Connect(); break;
				case NECommand.Database_Examine: ConfigureExecute_Database_Examine(); break;
				case NECommand.Select_Limit: ConfigureExecute_Select_Limit(); break;
				case NECommand.Select_RepeatsCaseSensitive_ByCount: ConfigureExecute_Select_Repeats_ByCount(); break;
				case NECommand.Select_RepeatsCaseInsensitive_ByCount: ConfigureExecute_Select_Repeats_ByCount(); break;
				case NECommand.Select_Split: ConfigureExecute_Select_Split(); break;
			}
		}
		#endregion

		#region Execute
		public void Execute()
		{
			switch (state.Command)
			{
				case NECommand.Internal_Key: Execute_Internal_Key(); break;
				case NECommand.Internal_Text: Execute_Internal_Text(); break;
				case NECommand.File_New_FromSelections: Execute_File_New_FromSelections(); break;
				case NECommand.File_Open_Selected: Execute_File_Open_Selected(); break;
				case NECommand.File_Save_Save: Execute_File_Save_Save(); break;
				case NECommand.File_Save_SaveAs: Execute_File_SaveCopy_SaveCopy(); break;
				case NECommand.File_Save_SaveAsClipboard: Execute_File_SaveCopy_SaveCopyClipboard(); break;
				case NECommand.File_Save_SaveAsByExpression: Execute_File_SaveCopy_SaveCopyByExpression(); break;
				case NECommand.File_Copy_CopyTo: Execute_File_SaveCopy_SaveCopy(true); break;
				case NECommand.File_Copy_CopyToClipboard: Execute_File_SaveCopy_SaveCopyClipboard(true); break;
				case NECommand.File_Copy_CopyToByExpression: Execute_File_SaveCopy_SaveCopyByExpression(true); break;
				case NECommand.File_Copy_Path: Execute_File_Copy_Path(); break;
				case NECommand.File_Copy_Name: Execute_File_Copy_Name(); break;
				case NECommand.File_Copy_DisplayName: Execute_File_Copy_DisplayName(); break;
				case NECommand.File_Operations_Rename: Execute_File_Operations_Rename(); break;
				case NECommand.File_Operations_RenameClipboard: Execute_File_Operations_RenameClipboard(); break;
				case NECommand.File_Operations_RenameByExpression: Execute_File_Operations_RenameByExpression(); break;
				case NECommand.File_Operations_Delete: Execute_File_Operations_Delete(); break;
				case NECommand.File_Operations_Explore: Execute_File_Operations_Explore(); break;
				case NECommand.File_Operations_CommandPrompt: Execute_File_Operations_CommandPrompt(); break;
				case NECommand.File_Operations_VCSDiff: Execute_File_Operations_VCSDiff(); break;
				case NECommand.File_Operations_SetDisplayName: Execute_File_Operations_SetDisplayName(); break;
				case NECommand.File_Close: Execute_File_Close(); break;
				case NECommand.File_Refresh: Execute_File_Refresh(); break;
				case NECommand.File_AutoRefresh: Execute_File_AutoRefresh(); break;
				case NECommand.File_Revert: Execute_File_Revert(); break;
				case NECommand.File_Insert_Files: Execute_File_Insert_Files(); break;
				case NECommand.File_Insert_CopiedCut: Execute_File_Insert_CopiedCut(); break;
				case NECommand.File_Insert_Selected: Execute_File_Insert_Selected(); break;
				case NECommand.File_Encoding_Encoding: Execute_File_Encoding_Encoding(); break;
				case NECommand.File_Encoding_ReopenWithEncoding: Execute_File_Encoding_ReopenWithEncoding(); break;
				case NECommand.File_Encoding_LineEndings: Execute_File_Encoding_LineEndings(); break;
				case NECommand.File_Encrypt: Execute_File_Encrypt(); break;
				case NECommand.File_Compress: Execute_File_Compress(); break;
				case NECommand.Edit_Undo: Execute_Edit_Undo(); break;
				case NECommand.Edit_Redo: Execute_Edit_Redo(); break;
				case NECommand.Edit_Copy_Copy: Execute_Edit_Copy_CutCopy(false); break;
				case NECommand.Edit_Copy_Cut: Execute_Edit_Copy_CutCopy(true); break;
				case NECommand.Edit_Paste_Paste: Execute_Edit_Paste_Paste(state.ShiftDown, false); break;
				case NECommand.Edit_Paste_RotatePaste: Execute_Edit_Paste_Paste(true, true); break;
				case NECommand.Edit_Find_Find: Execute_Edit_Find_Find(); break;
				case NECommand.Edit_Find_RegexReplace: Execute_Edit_Find_RegexReplace(); break;
				case NECommand.Edit_CopyDown: Execute_Edit_CopyDown(); break;
				case NECommand.Edit_Rotate: Execute_Edit_Rotate(); break;
				case NECommand.Edit_Repeat: Execute_Edit_Repeat(); break;
				case NECommand.Edit_Escape_Markup: Execute_Edit_Escape_Markup(); break;
				case NECommand.Edit_Escape_RegEx: Execute_Edit_Escape_RegEx(); break;
				case NECommand.Edit_Escape_URL: Execute_Edit_Escape_URL(); break;
				case NECommand.Edit_Unescape_Markup: Execute_Edit_Unescape_Markup(); break;
				case NECommand.Edit_Unescape_RegEx: Execute_Edit_Unescape_RegEx(); break;
				case NECommand.Edit_Unescape_URL: Execute_Edit_Unescape_URL(); break;
				case NECommand.Edit_Data_Hash: Execute_Edit_Data_Hash(); break;
				case NECommand.Edit_Data_Compress: Execute_Edit_Data_Compress(); break;
				case NECommand.Edit_Data_Decompress: Execute_Edit_Data_Decompress(); break;
				case NECommand.Edit_Data_Encrypt: Execute_Edit_Data_Encrypt(); break;
				case NECommand.Edit_Data_Decrypt: Execute_Edit_Data_Decrypt(); break;
				case NECommand.Edit_Data_Sign: Execute_Edit_Data_Sign(); break;
				case NECommand.Edit_Sort: Execute_Edit_Sort(); break;
				case NECommand.Edit_Convert: Execute_Edit_Convert(); break;
				case NECommand.Edit_ModifyRegions: Execute_Edit_ModifyRegions(state.ConfigureExecuteData as EditModifyRegionsDialog.Result); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region1: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Select, 1); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region2: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Select, 2); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region3: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Select, 3); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region4: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Select, 4); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region5: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Select, 5); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region6: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Select, 6); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region7: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Select, 7); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region8: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Select, 8); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region9: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Select, 9); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region1: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Previous, 1); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region2: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Previous, 2); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region3: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Previous, 3); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region4: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Previous, 4); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region5: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Previous, 5); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region6: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Previous, 6); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region7: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Previous, 7); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region8: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Previous, 8); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region9: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Previous, 9); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region1: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Next, 1); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region2: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Next, 2); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region3: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Next, 3); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region4: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Next, 4); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region5: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Next, 5); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region6: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Next, 6); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region7: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Next, 7); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region8: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Next, 8); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region9: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Next, 9); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region1: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Enclosing, 1); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region2: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Enclosing, 2); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region3: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Enclosing, 3); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region4: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Enclosing, 4); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region5: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Enclosing, 5); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region6: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Enclosing, 6); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region7: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Enclosing, 7); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region8: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Enclosing, 8); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region9: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Enclosing, 9); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region1: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithEnclosing, 1); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region2: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithEnclosing, 2); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region3: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithEnclosing, 3); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region4: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithEnclosing, 4); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region5: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithEnclosing, 5); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region6: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithEnclosing, 6); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region7: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithEnclosing, 7); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region8: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithEnclosing, 8); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region9: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithEnclosing, 9); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region1: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithoutEnclosing, 1); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region2: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithoutEnclosing, 2); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region3: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithoutEnclosing, 3); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region4: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithoutEnclosing, 4); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region5: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithoutEnclosing, 5); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region6: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithoutEnclosing, 6); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region7: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithoutEnclosing, 7); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region8: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithoutEnclosing, 8); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region9: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithoutEnclosing, 9); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region1: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Set, 1); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region2: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Set, 2); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region3: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Set, 3); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region4: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Set, 4); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region5: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Set, 5); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region6: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Set, 6); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region7: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Set, 7); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region8: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Set, 8); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region9: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Set, 9); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region1: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Clear, 1); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region2: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Clear, 2); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region3: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Clear, 3); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region4: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Clear, 4); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region5: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Clear, 5); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region6: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Clear, 6); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region7: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Clear, 7); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region8: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Clear, 8); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region9: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Clear, 9); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region1: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Remove, 1); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region2: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Remove, 2); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region3: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Remove, 3); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region4: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Remove, 4); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region5: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Remove, 5); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region6: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Remove, 6); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region7: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Remove, 7); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region8: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Remove, 8); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region9: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Remove, 9); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region1: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Add, 1); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region2: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Add, 2); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region3: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Add, 3); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region4: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Add, 4); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region5: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Add, 5); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region6: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Add, 6); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region7: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Add, 7); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region8: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Add, 8); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region9: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Add, 9); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region1: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Unite, 1); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region2: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Unite, 2); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region3: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Unite, 3); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region4: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Unite, 4); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region5: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Unite, 5); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region6: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Unite, 6); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region7: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Unite, 7); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region8: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Unite, 8); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region9: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Unite, 9); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region1: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Intersect, 1); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region2: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Intersect, 2); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region3: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Intersect, 3); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region4: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Intersect, 4); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region5: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Intersect, 5); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region6: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Intersect, 6); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region7: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Intersect, 7); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region8: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Intersect, 8); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region9: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Intersect, 9); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region1: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Exclude, 1); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region2: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Exclude, 2); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region3: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Exclude, 3); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region4: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Exclude, 4); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region5: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Exclude, 5); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region6: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Exclude, 6); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region7: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Exclude, 7); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region8: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Exclude, 8); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region9: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Exclude, 9); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region1: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Repeat, 1); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region2: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Repeat, 2); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region3: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Repeat, 3); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region4: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Repeat, 4); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region5: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Repeat, 5); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region6: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Repeat, 6); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region7: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Repeat, 7); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region8: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Repeat, 8); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region9: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Repeat, 9); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region1: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_Enclosing, 1); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region2: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_Enclosing, 2); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region3: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_Enclosing, 3); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region4: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_Enclosing, 4); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region5: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_Enclosing, 5); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region6: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_Enclosing, 6); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region7: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_Enclosing, 7); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region8: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_Enclosing, 8); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region9: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_Enclosing, 9); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region1: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_EnclosingIndex, 1); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region2: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_EnclosingIndex, 2); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region3: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_EnclosingIndex, 3); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region4: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_EnclosingIndex, 4); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region5: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_EnclosingIndex, 5); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region6: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_EnclosingIndex, 6); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region7: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_EnclosingIndex, 7); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region8: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_EnclosingIndex, 8); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region9: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_EnclosingIndex, 9); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region1: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Flatten, 1); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region2: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Flatten, 2); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region3: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Flatten, 3); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region4: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Flatten, 4); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region5: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Flatten, 5); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region6: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Flatten, 6); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region7: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Flatten, 7); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region8: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Flatten, 8); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region9: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Flatten, 9); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region1: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Transpose, 1); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region2: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Transpose, 2); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region3: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Transpose, 3); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region4: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Transpose, 4); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region5: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Transpose, 5); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region6: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Transpose, 6); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region7: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Transpose, 7); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region8: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Transpose, 8); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region9: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Transpose, 9); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region1: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateLeft, 1); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region2: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateLeft, 2); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region3: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateLeft, 3); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region4: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateLeft, 4); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region5: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateLeft, 5); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region6: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateLeft, 6); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region7: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateLeft, 7); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region8: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateLeft, 8); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region9: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateLeft, 9); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region1: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateRight, 1); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region2: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateRight, 2); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region3: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateRight, 3); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region4: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateRight, 4); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region5: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateRight, 5); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region6: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateRight, 6); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region7: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateRight, 7); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region8: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateRight, 8); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region9: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateRight, 9); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region1: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Rotate180, 1); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region2: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Rotate180, 2); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region3: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Rotate180, 3); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region4: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Rotate180, 4); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region5: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Rotate180, 5); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region6: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Rotate180, 6); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region7: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Rotate180, 7); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region8: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Rotate180, 8); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region9: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Rotate180, 9); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region1: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorHorizontal, 1); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region2: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorHorizontal, 2); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region3: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorHorizontal, 3); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region4: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorHorizontal, 4); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region5: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorHorizontal, 5); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region6: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorHorizontal, 6); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region7: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorHorizontal, 7); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region8: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorHorizontal, 8); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region9: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorHorizontal, 9); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region1: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorVertical, 1); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region2: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorVertical, 2); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region3: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorVertical, 3); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region4: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorVertical, 4); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region5: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorVertical, 5); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region6: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorVertical, 6); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region7: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorVertical, 7); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region8: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorVertical, 8); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region9: Execute_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorVertical, 9); break;
				case NECommand.Edit_Navigate_WordLeft: Execute_Edit_Navigate_WordLeftRight(false); break;
				case NECommand.Edit_Navigate_WordRight: Execute_Edit_Navigate_WordLeftRight(true); break;
				case NECommand.Edit_Navigate_AllLeft: Execute_Edit_Navigate_AllLeft(); break;
				case NECommand.Edit_Navigate_AllRight: Execute_Edit_Navigate_AllRight(); break;
				case NECommand.Edit_Navigate_JumpBy_Words: Execute_Edit_Navigate_JumpBy(JumpByType.Words); break;
				case NECommand.Edit_Navigate_JumpBy_Numbers: Execute_Edit_Navigate_JumpBy(JumpByType.Numbers); break;
				case NECommand.Edit_Navigate_JumpBy_Paths: Execute_Edit_Navigate_JumpBy(JumpByType.Paths); break;
				//TODO
				//case NECommand.Diff_Selections: Command_Diff_Selections(); break;
				//case NECommand.Diff_SelectedFiles: Command_Diff_SelectedFiles(); break;
				//case NECommand.Diff_VCSNormalFiles: Command_Diff_Diff_VCSNormalFiles(); break;
				//case NECommand.Diff_Regions_Region1: Command_Diff_Regions_Region(1); break;
				//case NECommand.Diff_Regions_Region2: Command_Diff_Regions_Region(2); break;
				//case NECommand.Diff_Regions_Region3: Command_Diff_Regions_Region(3); break;
				//case NECommand.Diff_Regions_Region4: Command_Diff_Regions_Region(4); break;
				//case NECommand.Diff_Regions_Region5: Command_Diff_Regions_Region(5); break;
				//case NECommand.Diff_Regions_Region6: Command_Diff_Regions_Region(6); break;
				//case NECommand.Diff_Regions_Region7: Command_Diff_Regions_Region(7); break;
				//case NECommand.Diff_Regions_Region8: Command_Diff_Regions_Region(8); break;
				//case NECommand.Diff_Regions_Region9: Command_Diff_Regions_Region(9); break;
				//case NECommand.Diff_Break: Command_Diff_Break(); break;
				//case NECommand.Diff_IgnoreWhitespace: Command_Diff_IgnoreWhitespace(state.MultiStatus); break;
				//case NECommand.Diff_IgnoreCase: Command_Diff_IgnoreCase(state.MultiStatus); break;
				//case NECommand.Diff_IgnoreNumbers: Command_Diff_IgnoreNumbers(state.MultiStatus); break;
				//case NECommand.Diff_IgnoreLineEndings: Command_Diff_IgnoreLineEndings(state.MultiStatus); break;
				//case NECommand.Diff_IgnoreCharacters: Command_Diff_IgnoreCharacters(state.Parameters as DiffIgnoreCharactersDialog.Result); break;
				//case NECommand.Diff_Reset: Command_Diff_Reset(); break;
				//case NECommand.Diff_Next: Command_Diff_NextPrevious(true, state.ShiftDown); break;
				//case NECommand.Diff_Previous: Command_Diff_NextPrevious(false, state.ShiftDown); break;
				//case NECommand.Diff_CopyLeft: Command_Diff_CopyLeftRight(true); break;
				//case NECommand.Diff_CopyRight: Command_Diff_CopyLeftRight(false); break;
				//case NECommand.Diff_Fix_Whitespace: Command_Diff_Fix_Whitespace(state.Parameters as DiffFixWhitespaceDialog.Result); break;
				//case NECommand.Diff_Fix_Case: Command_Diff_Fix_Case(); break;
				//case NECommand.Diff_Fix_Numbers: Command_Diff_Fix_Numbers(); break;
				//case NECommand.Diff_Fix_LineEndings: Command_Diff_Fix_LineEndings(); break;
				//case NECommand.Diff_Fix_Encoding: Command_Diff_Fix_Encoding(); break;
				//case NECommand.Diff_Select_Match: Command_Diff_Select_MatchDiff(true); break;
				//case NECommand.Diff_Select_Diff: Command_Diff_Select_MatchDiff(false); break;
				case NECommand.Files_Name_Simplify: Execute_Files_Name_Simplify(); break;
				case NECommand.Files_Name_MakeAbsolute: Execute_Files_Name_MakeAbsolute(); break;
				case NECommand.Files_Name_MakeRelative: Execute_Files_Name_MakeRelative(); break;
				case NECommand.Files_Name_GetUnique: Execute_Files_Name_GetUnique(); break;
				case NECommand.Files_Name_Sanitize: Execute_Files_Name_Sanitize(); break;
				case NECommand.Files_Get_Size: Execute_Files_Get_Size(); break;
				case NECommand.Files_Get_Time_Write: Execute_Files_Get_Time(TimestampType.Write); break;
				case NECommand.Files_Get_Time_Access: Execute_Files_Get_Time(TimestampType.Access); break;
				case NECommand.Files_Get_Time_Create: Execute_Files_Get_Time(TimestampType.Create); break;
				case NECommand.Files_Get_Attributes: Execute_Files_Get_Attributes(); break;
				case NECommand.Files_Get_Version_File: Execute_Files_Get_Version_File(); break;
				case NECommand.Files_Get_Version_Product: Execute_Files_Get_Version_Product(); break;
				case NECommand.Files_Get_Version_Assembly: Execute_Files_Get_Version_Assembly(); break;
				case NECommand.Files_Get_Children: Execute_Files_Get_ChildrenDescendants(false); break;
				case NECommand.Files_Get_Descendants: Execute_Files_Get_ChildrenDescendants(true); break;
				case NECommand.Files_Get_VersionControlStatus: Execute_Files_Get_VersionControlStatus(); break;
				case NECommand.Files_Set_Size: Execute_Files_Set_Size(); break;
				case NECommand.Files_Set_Time_Write: Execute_Files_Set_Time(TimestampType.Write); break;
				case NECommand.Files_Set_Time_Access: Execute_Files_Set_Time(TimestampType.Access); break;
				case NECommand.Files_Set_Time_Create: Execute_Files_Set_Time(TimestampType.Create); break;
				case NECommand.Files_Set_Time_All: Execute_Files_Set_Time(TimestampType.All); break;
				case NECommand.Files_Set_Attributes: Execute_Files_Set_Attributes(); break;
				case NECommand.Files_Find: Execute_Files_Find(); break;
				case NECommand.Files_Insert: Execute_Files_Insert(); break;
				case NECommand.Files_Create_Files: Execute_Files_Create_Files(); break;
				case NECommand.Files_Create_Directories: Execute_Files_Create_Directories(); break;
				case NECommand.Files_Create_FromExpressions: Execute_Files_Create_FromExpressions(); break;
				case NECommand.Files_Select_Name_Directory: Execute_Files_Select_Name(GetPathType.Directory); break;
				case NECommand.Files_Select_Name_Name: Execute_Files_Select_Name(GetPathType.FileName); break;
				case NECommand.Files_Select_Name_FileNamewoExtension: Execute_Files_Select_Name(GetPathType.FileNameWoExtension); break;
				case NECommand.Files_Select_Name_Extension: Execute_Files_Select_Name(GetPathType.Extension); break;
				case NECommand.Files_Select_Name_Next: Execute_Files_Select_Name_Next(); break;
				case NECommand.Files_Select_Files: Execute_Files_Select_Files(); break;
				case NECommand.Files_Select_Directories: Execute_Files_Select_Directories(); break;
				case NECommand.Files_Select_Existing: Execute_Files_Select_Existing(true); break;
				case NECommand.Files_Select_NonExisting: Execute_Files_Select_Existing(false); break;
				case NECommand.Files_Select_Roots: Execute_Files_Select_Roots(true); break;
				case NECommand.Files_Select_NonRoots: Execute_Files_Select_Roots(false); break;
				case NECommand.Files_Select_MatchDepth: Execute_Files_Select_MatchDepth(); break;
				case NECommand.Files_Select_CommonAncestor: Execute_Files_Select_CommonAncestor(); break;
				case NECommand.Files_Select_ByVersionControlStatus: Execute_Files_Select_ByVersionControlStatus(); break;
				case NECommand.Files_Hash: Execute_Files_Hash(); break;
				case NECommand.Files_Sign: Execute_Files_Sign(); break;
				case NECommand.Files_Operations_Copy: Execute_Files_Operations_CopyMove(false); break;
				case NECommand.Files_Operations_Move: Execute_Files_Operations_CopyMove(true); break;
				case NECommand.Files_Operations_Delete: Execute_Files_Operations_Delete(); break;
				case NECommand.Files_Operations_DragDrop: Execute_Files_Operations_DragDrop(); break;
				case NECommand.Files_Operations_Explore: Execute_Files_Operations_Explore(); break;
				case NECommand.Files_Operations_CommandPrompt: Execute_Files_Operations_CommandPrompt(); break;
				case NECommand.Files_Operations_RunCommand_Parallel: Execute_Files_Operations_RunCommand_Parallel(); break;
				case NECommand.Files_Operations_RunCommand_Sequential: Execute_Files_Operations_RunCommand_Sequential(); break;
				case NECommand.Files_Operations_RunCommand_Shell: Execute_Files_Operations_RunCommand_Shell(); break;
				case NECommand.Files_Operations_Encoding: Execute_Files_Operations_Encoding(); break;
				case NECommand.Files_Operations_SplitFile: Execute_Files_Operations_SplitFile(); break;
				case NECommand.Files_Operations_CombineFiles: Execute_Files_Operations_CombineFiles(); break;
				case NECommand.Edit_Expression_Expression: Execute_Edit_Expression_Expression(); break;
				case NECommand.Edit_Expression_EvaluateSelected: Execute_Edit_Expression_EvaluateSelected(); break;
				case NECommand.Text_Select_Trim: Execute_Text_Select_Trim(); break;
				case NECommand.Text_Select_ByWidth: Execute_Text_Select_ByWidth(); break;
				case NECommand.Text_Select_WholeWord: Execute_Text_Select_WholeBoundedWord(true); break;
				case NECommand.Text_Select_BoundedWord: Execute_Text_Select_WholeBoundedWord(false); break;
				case NECommand.Text_Select_Min_Text: Execute_Text_Select_MinMax_Text(false); break;
				case NECommand.Text_Select_Min_Length: Execute_Text_Select_MinMax_Length(false); break;
				case NECommand.Text_Select_Max_Text: Execute_Text_Select_MinMax_Text(true); break;
				case NECommand.Text_Select_Max_Length: Execute_Text_Select_MinMax_Length(true); break;
				case NECommand.Text_Case_Upper: Execute_Text_Case_Upper(); break;
				case NECommand.Text_Case_Lower: Execute_Text_Case_Lower(); break;
				case NECommand.Text_Case_Proper: Execute_Text_Case_Proper(); break;
				case NECommand.Text_Case_Toggle: Execute_Text_Case_Toggle(); break;
				case NECommand.Text_Length: Execute_Text_Length(); break;
				case NECommand.Text_Width: Execute_Text_Width(); break;
				case NECommand.Text_Trim: Execute_Text_Trim(); break;
				case NECommand.Text_SingleLine: Execute_Text_SingleLine(); break;
				case NECommand.Text_Unicode: Execute_Text_Unicode(); break;
				case NECommand.Text_GUID: Execute_Text_GUID(); break;
				case NECommand.Text_RandomText: Execute_Text_RandomText(); break;
				case NECommand.Text_LoremIpsum: Execute_Text_LoremIpsum(); break;
				case NECommand.Text_ReverseRegEx: Execute_Text_ReverseRegEx(); break;
				case NECommand.Text_FirstDistinct: Execute_Text_FirstDistinct(); break;
				case NECommand.Text_RepeatCount: Execute_Text_RepeatCount(); break;
				case NECommand.Text_RepeatIndex: Execute_Text_RepeatIndex(); break;
				case NECommand.Numeric_Select_Min: Execute_Numeric_Select_MinMax(false); break;
				case NECommand.Numeric_Select_Max: Execute_Numeric_Select_MinMax(true); break;
				case NECommand.Numeric_Select_Fraction_Whole: Execute_Numeric_Select_Fraction_Whole(); break;
				case NECommand.Numeric_Select_Fraction_Fraction: Execute_Numeric_Select_Fraction_Fraction(); break;
				case NECommand.Numeric_Hex_ToHex: Execute_Numeric_Hex_ToHex(); break;
				case NECommand.Numeric_Hex_FromHex: Execute_Numeric_Hex_FromHex(); break;
				case NECommand.Numeric_ConvertBase: Execute_Numeric_ConvertBase(); break;
				case NECommand.Numeric_Series_ZeroBased: Execute_Numeric_Series_ZeroBased(); break;
				case NECommand.Numeric_Series_OneBased: Execute_Numeric_Series_OneBased(); break;
				case NECommand.Numeric_Series_Linear: Execute_Numeric_Series_LinearGeometric(true); break;
				case NECommand.Numeric_Series_Geometric: Execute_Numeric_Series_LinearGeometric(false); break;
				case NECommand.Numeric_Scale: Execute_Numeric_Scale(); break;
				case NECommand.Numeric_Add_Sum: Execute_Numeric_Add_Sum(); break;
				case NECommand.Numeric_Add_ForwardSum: Execute_Numeric_Add_ForwardReverseSum(true, false); break;
				case NECommand.Numeric_Add_ReverseSum: Execute_Numeric_Add_ForwardReverseSum(false, false); break;
				case NECommand.Numeric_Add_UndoForwardSum: Execute_Numeric_Add_ForwardReverseSum(true, true); break;
				case NECommand.Numeric_Add_UndoReverseSum: Execute_Numeric_Add_ForwardReverseSum(false, true); break;
				case NECommand.Numeric_Add_Increment: Execute_Numeric_Add_IncrementDecrement(true); break;
				case NECommand.Numeric_Add_Decrement: Execute_Numeric_Add_IncrementDecrement(false); break;
				case NECommand.Numeric_Add_AddClipboard: Execute_Numeric_Add_AddSubtractClipboard(true); break;
				case NECommand.Numeric_Add_SubtractClipboard: Execute_Numeric_Add_AddSubtractClipboard(false); break;
				case NECommand.Numeric_Fraction_Whole: Execute_Numeric_Fraction_Whole(); break;
				case NECommand.Numeric_Fraction_Fraction: Execute_Numeric_Fraction_Fraction(); break;
				case NECommand.Numeric_Fraction_Simplify: Execute_Numeric_Fraction_Simplify(); break;
				case NECommand.Numeric_Absolute: Execute_Numeric_Absolute(); break;
				case NECommand.Numeric_Floor: Execute_Numeric_Floor(); break;
				case NECommand.Numeric_Ceiling: Execute_Numeric_Ceiling(); break;
				case NECommand.Numeric_Round: Execute_Numeric_Round(); break;
				case NECommand.Numeric_Limit: Execute_Numeric_Limit(); break;
				case NECommand.Numeric_Cycle: Execute_Numeric_Cycle(); break;
				case NECommand.Numeric_Trim: Execute_Numeric_Trim(); break;
				case NECommand.Numeric_Factor: Execute_Numeric_Factor(); break;
				case NECommand.Numeric_RandomNumber: Execute_Numeric_RandomNumber(); break;
				case NECommand.Numeric_CombinationsPermutations: Execute_Numeric_CombinationsPermutations(); break;
				case NECommand.Numeric_MinMaxValues: Execute_Numeric_MinMaxValues(); break;
				case NECommand.DateTime_Now: Execute_DateTime_Now(); break;
				case NECommand.DateTime_UtcNow: Execute_DateTime_UtcNow(); break;
				case NECommand.DateTime_Format: Execute_DateTime_Format(); break;
				case NECommand.DateTime_ToUtc: Execute_DateTime_ToUtc(); break;
				case NECommand.DateTime_ToLocal: Execute_DateTime_ToLocal(); break;
				case NECommand.DateTime_ToTimeZone: Execute_DateTime_ToTimeZone(); break;
				case NECommand.DateTime_AddClipboard: Execute_DateTime_AddClipboard(); break;
				case NECommand.DateTime_SubtractClipboard: Execute_DateTime_SubtractClipboard(); break;
				case NECommand.Image_GrabColor: Execute_Image_GrabColor(); break;
				case NECommand.Image_GrabImage: Execute_Image_GrabImage(); break;
				case NECommand.Image_AdjustColor: Execute_Image_AdjustColor(); break;
				case NECommand.Image_AddColor: Execute_Image_AddColor(); break;
				case NECommand.Image_OverlayColor: Execute_Image_OverlayColor(); break;
				case NECommand.Image_Size: Execute_Image_Size(); break;
				case NECommand.Image_Crop: Execute_Image_Crop(); break;
				case NECommand.Image_FlipHorizontal: Execute_Image_FlipHorizontal(); break;
				case NECommand.Image_FlipVertical: Execute_Image_FlipVertical(); break;
				case NECommand.Image_Rotate: Execute_Image_Rotate(); break;
				case NECommand.Image_GIF_Animate: Execute_Image_GIF_Animate(); break;
				case NECommand.Image_GIF_Split: Execute_Image_GIF_Split(); break;
				case NECommand.Table_DetectType: Execute_Table_DetectType(); break;
				case NECommand.Table_Convert: Execute_Table_Convert(); break;
				case NECommand.Table_TextToTable: Execute_Table_TextToTable(); break;
				case NECommand.Table_LineSelectionsToTable: Execute_Table_LineSelectionsToTable(); break;
				case NECommand.Table_RegionSelectionsToTable_Region1: Execute_Table_RegionSelectionsToTable_Region(1); break;
				case NECommand.Table_RegionSelectionsToTable_Region2: Execute_Table_RegionSelectionsToTable_Region(2); break;
				case NECommand.Table_RegionSelectionsToTable_Region3: Execute_Table_RegionSelectionsToTable_Region(3); break;
				case NECommand.Table_RegionSelectionsToTable_Region4: Execute_Table_RegionSelectionsToTable_Region(4); break;
				case NECommand.Table_RegionSelectionsToTable_Region5: Execute_Table_RegionSelectionsToTable_Region(5); break;
				case NECommand.Table_RegionSelectionsToTable_Region6: Execute_Table_RegionSelectionsToTable_Region(6); break;
				case NECommand.Table_RegionSelectionsToTable_Region7: Execute_Table_RegionSelectionsToTable_Region(7); break;
				case NECommand.Table_RegionSelectionsToTable_Region8: Execute_Table_RegionSelectionsToTable_Region(8); break;
				case NECommand.Table_RegionSelectionsToTable_Region9: Execute_Table_RegionSelectionsToTable_Region(9); break;
				case NECommand.Table_EditTable: Execute_Table_EditTable(); break;
				case NECommand.Table_AddHeaders: Execute_Table_AddHeaders(); break;
				case NECommand.Table_AddRow: Execute_Table_AddRow(); break;
				case NECommand.Table_AddColumn: Execute_Table_AddColumn(); break;
				case NECommand.Table_Select_RowsByExpression: Execute_Table_Select_RowsByExpression(); break;
				case NECommand.Table_SetJoinSource: Execute_Table_SetJoinSource(); break;
				case NECommand.Table_Join: Execute_Table_Join(); break;
				case NECommand.Table_Transpose: Execute_Table_Transpose(); break;
				case NECommand.Table_Database_GenerateInserts: Execute_Table_Database_GenerateInserts(); break;
				case NECommand.Table_Database_GenerateUpdates: Execute_Table_Database_GenerateUpdates(); break;
				case NECommand.Table_Database_GenerateDeletes: Execute_Table_Database_GenerateDeletes(); break;
				case NECommand.Position_Goto_Lines: Execute_Position_Goto(GotoType.Line, state.ShiftDown); break;
				case NECommand.Position_Goto_Columns: Execute_Position_Goto(GotoType.Column, state.ShiftDown); break;
				case NECommand.Position_Goto_Indexes: Execute_Position_Goto(GotoType.Index, state.ShiftDown); break;
				case NECommand.Position_Goto_Positions: Execute_Position_Goto(GotoType.Position, state.ShiftDown); break;
				case NECommand.Position_Copy_Lines: Execute_Position_Copy(GotoType.Line, false); break;
				case NECommand.Position_Copy_Columns: Execute_Position_Copy(GotoType.Column, !state.ShiftDown); break;
				case NECommand.Position_Copy_Indexes: Execute_Position_Copy(GotoType.Index, !state.ShiftDown); break;
				case NECommand.Position_Copy_Positions: Execute_Position_Copy(GotoType.Position, false); break;
				case NECommand.Content_Type_SetFromExtension: Execute_Content_Type_SetFromExtension(); break;
				case NECommand.Content_Type_None: Execute_Content_Type(ParserType.None); break;
				case NECommand.Content_Type_Balanced: Execute_Content_Type(ParserType.Balanced); break;
				case NECommand.Content_Type_Columns: Execute_Content_Type(ParserType.Columns); break;
				case NECommand.Content_Type_CPlusPlus: Execute_Content_Type(ParserType.CPlusPlus); break;
				case NECommand.Content_Type_CSharp: Execute_Content_Type(ParserType.CSharp); break;
				case NECommand.Content_Type_CSV: Execute_Content_Type(ParserType.CSV); break;
				case NECommand.Content_Type_ExactColumns: Execute_Content_Type(ParserType.ExactColumns); break;
				case NECommand.Content_Type_HTML: Execute_Content_Type(ParserType.HTML); break;
				case NECommand.Content_Type_JSON: Execute_Content_Type(ParserType.JSON); break;
				case NECommand.Content_Type_SQL: Execute_Content_Type(ParserType.SQL); break;
				case NECommand.Content_Type_TSV: Execute_Content_Type(ParserType.TSV); break;
				case NECommand.Content_Type_XML: Execute_Content_Type(ParserType.XML); break;
				case NECommand.Content_HighlightSyntax: Execute_Content_HighlightSyntax(); break;
				case NECommand.Content_StrictParsing: Execute_Content_StrictParsing(); break;
				case NECommand.Content_Reformat: Execute_Content_Reformat(); break;
				case NECommand.Content_Comment: Execute_Content_Comment(); break;
				case NECommand.Content_Uncomment: Execute_Content_Uncomment(); break;
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
				case NECommand.Content_Navigate_Up: Execute_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.Up, state.ShiftDown); break;
				case NECommand.Content_Navigate_Down: Execute_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.Down, state.ShiftDown); break;
				case NECommand.Content_Navigate_Left: Execute_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.Left, state.ShiftDown); break;
				case NECommand.Content_Navigate_Right: Execute_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.Right, state.ShiftDown); break;
				case NECommand.Content_Navigate_Home: Execute_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.Home, state.ShiftDown); break;
				case NECommand.Content_Navigate_End: Execute_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.End, state.ShiftDown); break;
				case NECommand.Content_Navigate_PgUp: Execute_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.PgUp, state.ShiftDown); break;
				case NECommand.Content_Navigate_PgDn: Execute_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.PgDn, state.ShiftDown); break;
				case NECommand.Content_Navigate_Row: Execute_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.Row, true); break;
				case NECommand.Content_Navigate_Column: Execute_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.Column, true); break;
				case NECommand.Content_KeepSelections: Execute_Content_KeepSelections(); break;
				case NECommand.Network_AbsoluteURL: Execute_Network_AbsoluteURL(); break;
				case NECommand.Network_Fetch: Execute_Network_Fetch(); break;
				case NECommand.Network_FetchHex: Execute_Network_Fetch(Coder.CodePage.Hex); break;
				case NECommand.Network_FetchFile: Execute_Network_FetchFile(); break;
				case NECommand.Network_FetchStream: Execute_Network_FetchStream(); break;
				case NECommand.Network_FetchPlaylist: Execute_Network_FetchPlaylist(); break;
				case NECommand.Network_Lookup_IP: Execute_Network_Lookup_IP(); break;
				case NECommand.Network_Lookup_HostName: Execute_Network_Lookup_HostName(); break;
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
				case NECommand.Keys_Set_KeysCaseSensitive: Execute_Keys_Set(0, true); break;
				case NECommand.Keys_Set_KeysCaseInsensitive: Execute_Keys_Set(0, false); break;
				case NECommand.Keys_Set_Values1: Execute_Keys_Set(1); break;
				case NECommand.Keys_Set_Values2: Execute_Keys_Set(2); break;
				case NECommand.Keys_Set_Values3: Execute_Keys_Set(3); break;
				case NECommand.Keys_Set_Values4: Execute_Keys_Set(4); break;
				case NECommand.Keys_Set_Values5: Execute_Keys_Set(5); break;
				case NECommand.Keys_Set_Values6: Execute_Keys_Set(6); break;
				case NECommand.Keys_Set_Values7: Execute_Keys_Set(7); break;
				case NECommand.Keys_Set_Values8: Execute_Keys_Set(8); break;
				case NECommand.Keys_Set_Values9: Execute_Keys_Set(9); break;
				case NECommand.Keys_Add_Keys: Execute_Keys_Add(0); break;
				case NECommand.Keys_Add_Values1: Execute_Keys_Add(1); break;
				case NECommand.Keys_Add_Values2: Execute_Keys_Add(2); break;
				case NECommand.Keys_Add_Values3: Execute_Keys_Add(3); break;
				case NECommand.Keys_Add_Values4: Execute_Keys_Add(4); break;
				case NECommand.Keys_Add_Values5: Execute_Keys_Add(5); break;
				case NECommand.Keys_Add_Values6: Execute_Keys_Add(6); break;
				case NECommand.Keys_Add_Values7: Execute_Keys_Add(7); break;
				case NECommand.Keys_Add_Values8: Execute_Keys_Add(8); break;
				case NECommand.Keys_Add_Values9: Execute_Keys_Add(9); break;
				case NECommand.Keys_Remove_Keys: Execute_Keys_Remove(0); break;
				case NECommand.Keys_Remove_Values1: Execute_Keys_Remove(1); break;
				case NECommand.Keys_Remove_Values2: Execute_Keys_Remove(2); break;
				case NECommand.Keys_Remove_Values3: Execute_Keys_Remove(3); break;
				case NECommand.Keys_Remove_Values4: Execute_Keys_Remove(4); break;
				case NECommand.Keys_Remove_Values5: Execute_Keys_Remove(5); break;
				case NECommand.Keys_Remove_Values6: Execute_Keys_Remove(6); break;
				case NECommand.Keys_Remove_Values7: Execute_Keys_Remove(7); break;
				case NECommand.Keys_Remove_Values8: Execute_Keys_Remove(8); break;
				case NECommand.Keys_Remove_Values9: Execute_Keys_Remove(9); break;
				case NECommand.Keys_Replace_Values1: Execute_Keys_Replace(1); break;
				case NECommand.Keys_Replace_Values2: Execute_Keys_Replace(2); break;
				case NECommand.Keys_Replace_Values3: Execute_Keys_Replace(3); break;
				case NECommand.Keys_Replace_Values4: Execute_Keys_Replace(4); break;
				case NECommand.Keys_Replace_Values5: Execute_Keys_Replace(5); break;
				case NECommand.Keys_Replace_Values6: Execute_Keys_Replace(6); break;
				case NECommand.Keys_Replace_Values7: Execute_Keys_Replace(7); break;
				case NECommand.Keys_Replace_Values8: Execute_Keys_Replace(8); break;
				case NECommand.Keys_Replace_Values9: Execute_Keys_Replace(9); break;
				case NECommand.Select_All: Execute_Select_All(); break;
				case NECommand.Select_Nothing: Execute_Select_Nothing(); break;
				case NECommand.Select_Limit: Execute_Select_Limit(); break;
				case NECommand.Select_Lines: Execute_Select_Lines(); break;
				case NECommand.Select_WholeLines: Execute_Select_WholeLines(); break;
				case NECommand.Select_Rectangle: Execute_Select_Rectangle(); break;
				case NECommand.Select_Invert: Execute_Select_Invert(); break;
				case NECommand.Select_Join: Execute_Select_Join(); break;
				case NECommand.Select_Empty: Execute_Select_Empty(true); break;
				case NECommand.Select_NonEmpty: Execute_Select_Empty(false); break;
				case NECommand.Select_ToggleOpenClose: Execute_Select_ToggleOpenClose(); break;
				case NECommand.Select_RepeatsCaseSensitive_Unique: Execute_Select_Repeats_Unique(true); break;
				case NECommand.Select_RepeatsCaseSensitive_Duplicates: Execute_Select_Repeats_Duplicates(true); break;
				case NECommand.Select_RepeatsCaseSensitive_MatchPrevious: Execute_Select_Repeats_MatchPrevious(true); break;
				case NECommand.Select_RepeatsCaseSensitive_NonMatchPrevious: Execute_Select_Repeats_NonMatchPrevious(true); break;
				case NECommand.Select_RepeatsCaseSensitive_RepeatedLines: Execute_Select_Repeats_RepeatedLines(true); break;
				case NECommand.Select_RepeatsCaseSensitive_ByCount: Execute_Select_Repeats_ByCount(true); break;
				case NECommand.Select_RepeatsCaseSensitive_Tabs_Match: Execute_Select_Repeats_Tabs_MatchMismatch(true); break;
				case NECommand.Select_RepeatsCaseSensitive_Tabs_Mismatch: Execute_Select_Repeats_Tabs_MatchMismatch(false); break;
				case NECommand.Select_RepeatsCaseSensitive_Tabs_Common: Execute_Select_Repeats_Tabs_CommonNonCommon(true); break;
				case NECommand.Select_RepeatsCaseSensitive_Tabs_NonCommon: Execute_Select_Repeats_Tabs_CommonNonCommon(false); break;
				case NECommand.Select_RepeatsCaseInsensitive_Unique: Execute_Select_Repeats_Unique(false); break;
				case NECommand.Select_RepeatsCaseInsensitive_Duplicates: Execute_Select_Repeats_Duplicates(false); break;
				case NECommand.Select_RepeatsCaseInsensitive_MatchPrevious: Execute_Select_Repeats_MatchPrevious(false); break;
				case NECommand.Select_RepeatsCaseInsensitive_NonMatchPrevious: Execute_Select_Repeats_NonMatchPrevious(false); break;
				case NECommand.Select_RepeatsCaseInsensitive_RepeatedLines: Execute_Select_Repeats_RepeatedLines(false); break;
				case NECommand.Select_RepeatsCaseInsensitive_ByCount: Execute_Select_Repeats_ByCount(false); break;
				case NECommand.Select_RepeatsCaseInsensitive_Tabs_Match: Execute_Select_Repeats_Tabs_MatchMismatch(true); break;
				case NECommand.Select_RepeatsCaseInsensitive_Tabs_Mismatch: Execute_Select_Repeats_Tabs_MatchMismatch(false); break;
				case NECommand.Select_RepeatsCaseInsensitive_Tabs_Common: Execute_Select_Repeats_Tabs_CommonNonCommon(true); break;
				case NECommand.Select_RepeatsCaseInsensitive_Tabs_NonCommon: Execute_Select_Repeats_Tabs_CommonNonCommon(false); break;
				case NECommand.Select_Split: Execute_Select_Split(); break;
				case NECommand.Select_Selection_First: Execute_Select_Selection_First(); break;
				case NECommand.Select_Selection_CenterVertically: Execute_Select_Selection_CenterVertically(); break;
				case NECommand.Select_Selection_Center: Execute_Select_Selection_Center(); break;
				case NECommand.Select_Selection_ToggleAnchor: Execute_Select_Selection_ToggleAnchor(); break;
				case NECommand.Select_Selection_Next: Execute_Select_Selection_NextPrevious(true); break;
				case NECommand.Select_Selection_Previous: Execute_Select_Selection_NextPrevious(false); break;
				case NECommand.Select_Selection_Single: Execute_Select_Selection_Single(); break;
				case NECommand.Select_Selection_Remove: Execute_Select_Selection_Remove(); break;
				case NECommand.Select_Selection_RemoveBeforeCurrent: Execute_Select_Selection_RemoveBeforeCurrent(); break;
				case NECommand.Select_Selection_RemoveAfterCurrent: Execute_Select_Selection_RemoveAfterCurrent(); break;
				//TODO
				//case NECommand.Macro_RepeatLastAction: Command_Macro_RepeatLastAction(); break;
				//case NECommand.Macro_TimeNextAction: timeNext = !timeNext; break;
				case NECommand.Window_TabIndex: Execute_Window_TabIndex(false); break;
				case NECommand.Window_ActiveTabIndex: Execute_Window_TabIndex(true); break;
				case NECommand.Window_ViewValues: Execute_Window_ViewValues(); break;
			}
		}

		WordSkipType GetWordSkipType(int position)
		{
			if ((position < 0) || (position >= Text.Length))
				return WordSkipType.Space;

			var c = Text[position];
			switch (JumpBy)
			{
				case JumpByType.Words:
				case JumpByType.Numbers:
					if (char.IsWhiteSpace(c))
						return WordSkipType.Space;
					else if ((char.IsLetterOrDigit(c)) || (c == '_') || ((JumpBy == JumpByType.Numbers) && ((c == '.') || (c == '-'))))
						return WordSkipType.Char;
					else
						return WordSkipType.Symbol;
				case JumpByType.Paths:
					if (c == '\\')
						return WordSkipType.Path;
					return WordSkipType.Char;
				default:
					return WordSkipType.Space;
			}
		}

		int GetNextWord(int position)
		{
			WordSkipType moveType = WordSkipType.None;

			--position;
			while (true)
			{
				if (position >= Text.Length)
					return Text.Length;

				++position;
				var current = GetWordSkipType(position);

				if (moveType == WordSkipType.None)
					moveType = current;

				if (current != moveType)
					return position;
			}
		}

		int GetPrevWord(int position)
		{
			WordSkipType moveType = WordSkipType.None;

			while (true)
			{
				if (position < 0)
					return 0;

				--position;
				var current = GetWordSkipType(position);

				if (moveType == WordSkipType.None)
					moveType = current;

				if (current != moveType)
					return position + 1;
			}
		}

		Range MoveCursor(Range range, int cursor, bool selecting)
		{
			cursor = Math.Max(0, Math.Min(cursor, Text.Length));
			if (selecting)
				if (range.Cursor == cursor)
					return range;
				else
					return new Range(cursor, range.Anchor);

			if ((range.Cursor == cursor) && (range.Anchor == cursor))
				return range;
			return new Range(cursor);
		}

		Range MoveCursor(Range range, int line, int index, bool selecting, bool lineRel = true, bool indexRel = true)
		{
			if ((lineRel) || (indexRel))
			{
				var startLine = TextView.GetPositionLine(range.Cursor);
				var startIndex = TextView.GetPositionIndex(range.Cursor, startLine);

				if (lineRel)
					line += startLine;
				if (indexRel)
					index += startIndex;
			}

			line = Math.Max(0, Math.Min(line, TextView.NumLines - 1));
			index = Math.Max(0, Math.Min(index, TextView.GetLineLength(line)));
			return MoveCursor(range, TextView.GetPosition(line, index), selecting);
		}

		void BlockSelDown()
		{
			var sels = new List<Range>();
			foreach (var range in Selections)
			{
				var cursorLine = TextView.GetPositionLine(range.Cursor);
				var highlightLine = TextView.GetPositionLine(range.Anchor);
				var cursorIndex = TextView.GetPositionIndex(range.Cursor, cursorLine);
				var highlightIndex = TextView.GetPositionIndex(range.Anchor, highlightLine);

				cursorLine = Math.Max(0, Math.Min(cursorLine + 1, TextView.NumLines - 1));
				highlightLine = Math.Max(0, Math.Min(highlightLine + 1, TextView.NumLines - 1));
				cursorIndex = Math.Max(0, Math.Min(cursorIndex, TextView.GetLineLength(cursorLine)));
				highlightIndex = Math.Max(0, Math.Min(highlightIndex, TextView.GetLineLength(highlightLine)));

				sels.Add(new Range(TextView.GetPosition(cursorLine, cursorIndex), TextView.GetPosition(highlightLine, highlightIndex)));
			}
			Selections = Selections.Concat(sels).ToList();
		}

		void BlockSelUp()
		{
			var found = new HashSet<string>();
			foreach (var range in Selections)
				found.Add(range.ToString());

			var sels = new List<Range>();
			foreach (var range in Selections)
			{
				var startLine = TextView.GetPositionLine(range.Start);
				var endLine = TextView.GetPositionLine(range.End);
				var startIndex = TextView.GetPositionIndex(range.Start, startLine);
				var endIndex = TextView.GetPositionIndex(range.End, endLine);

				startLine = Math.Max(0, Math.Min(startLine - 1, TextView.NumLines - 1));
				endLine = Math.Max(0, Math.Min(endLine - 1, TextView.NumLines - 1));
				startIndex = Math.Max(0, Math.Min(startIndex, TextView.GetLineLength(startLine)));
				endIndex = Math.Max(0, Math.Min(endIndex, TextView.GetLineLength(endLine)));

				var prevLineRange = new Range(TextView.GetPosition(startLine, startIndex), TextView.GetPosition(endLine, endIndex));
				if (found.Contains(prevLineRange.ToString()))
					sels.Add(prevLineRange);
				else
					sels.Add(range);
			}

			Selections = sels;
		}

		bool Execute_Internal_Key()
		{
			var ret = true;
			var key = (Key)state.ConfigureExecuteData;
			switch (key)
			{
				case Key.Back:
				case Key.Delete:
					{
						if ((bool)state.PreExecuteData)
						{
							ReplaceSelections("");
							break;
						}

						Replace(Selections.AsParallel().AsOrdered().Select(range =>
						{
							var position = range.Start;
							var anchor = range.Anchor;

							if (state.ControlDown)
							{
								if (key == Key.Back)
									position = GetPrevWord(position);
								else
									position = GetNextWord(position);
							}
							else if ((state.ShiftDown) && (key == Key.Delete))
							{
								var line = TextView.GetPositionLine(position);
								position = TextView.GetPosition(line, 0);
								anchor = position + TextView.GetLineLength(line) + TextView.GetEndingLength(line);
							}
							else
							{
								var line = TextView.GetPositionLine(position);
								var index = TextView.GetPositionIndex(position, line);

								if (key == Key.Back)
									--index;
								else
									++index;

								if (index < 0)
								{
									--line;
									if (line < 0)
										return null;
									index = TextView.GetLineLength(line);
								}
								if (index > TextView.GetLineLength(line))
								{
									++line;
									if (line >= TextView.NumLines)
										return null;
									index = 0;
								}

								position = TextView.GetPosition(line, index);
							}

							return new Range(position, anchor);
						}).Where(range => range != null).ToList(), null);
					}
					break;
				case Key.Escape:
					//DragFiles = null;
					if (Settings.EscapeClearsSelections)
					{
						//HandleCommand(NECommand.Select_Selection_Single, false, null, null, null);
						//if (!Selections.Any())
						//{
						//	var pos = TextView.GetPosition(Math.Max(0, Math.Min(YScrollValue, TextView.NumLines - 1)), 0);
						//	Selections = new List<Range> { new Range(pos) };
						//}
					}
					break;
				case Key.Left:
					{
						Selections = Selections.AsParallel().AsOrdered().Select(range =>
						{
							var line = TextView.GetPositionLine(range.Cursor);
							var index = TextView.GetPositionIndex(range.Cursor, line);
							if ((!state.ShiftDown) && ((bool)state.PreExecuteData))
								return new Range(range.Start);
							else if ((index == 0) && (line != 0))
								return MoveCursor(range, -1, int.MaxValue, state.ShiftDown, indexRel: false);
							else
								return MoveCursor(range, 0, -1, state.ShiftDown);
						}).ToList();
					}
					break;
				case Key.Right:
					{
						Selections = Selections.AsParallel().AsOrdered().Select(range =>
						{
							var line = TextView.GetPositionLine(range.Cursor);
							var index = TextView.GetPositionIndex(range.Cursor, line);
							if ((!state.ShiftDown) && ((bool)state.PreExecuteData))
								return new Range(range.End);
							else if ((index == TextView.GetLineLength(line)) && (line != TextView.NumLines - 1))
								return MoveCursor(range, 1, 0, state.ShiftDown, indexRel: false);
							else
								return MoveCursor(range, 0, 1, state.ShiftDown);
						}).ToList();
					}
					break;
				case Key.Up:
				case Key.Down:
					{
						var mult = key == Key.Up ? -1 : 1;
						if (!state.ControlDown)
							Selections = Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, mult, 0, state.ShiftDown)).ToList();
						else if (!state.ShiftDown)
							YScrollValue += mult;
						else if (key == Key.Down)
							BlockSelDown();
						else
							BlockSelUp();
					}
					break;
				case Key.Home:
					if (state.ControlDown)
					{
						var sels = Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, 0, state.ShiftDown)).ToList(); // Have to use MoveCursor for selection
						if ((!sels.Any()) && (!state.ShiftDown))
							sels.Add(new Range());
						Selections = sels;
					}
					else
					{
						var sels = new List<Range>();
						bool changed = false;
						foreach (var selection in Selections)
						{
							var line = TextView.GetPositionLine(selection.Cursor);
							var index = TextView.GetPositionIndex(selection.Cursor, line);

							int first;
							var end = TextView.GetLineLength(line);
							for (first = 0; first < end; ++first)
							{
								if (!char.IsWhiteSpace(Text[TextView.GetPosition(line, first)]))
									break;
							}
							if (first == end)
								first = 0;

							if (first != index)
								changed = true;
							sels.Add(MoveCursor(selection, 0, first, state.ShiftDown, indexRel: false));
						}
						if (!changed)
						{
							sels = sels.AsParallel().AsOrdered().Select(range => MoveCursor(range, 0, 0, state.ShiftDown, indexRel: false)).ToList();
							XScrollValue = 0;
						}
						Selections = sels;
					}
					break;
				case Key.End:
					if (state.ControlDown)
					{
						var sels = Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, Text.Length, state.ShiftDown)).ToList(); // Have to use MoveCursor for selection
						if ((!sels.Any()) && (!state.ShiftDown))
							sels.Add(new Range(Text.Length));
						Selections = sels;
					}
					else
						Selections = Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, 0, int.MaxValue, state.ShiftDown, indexRel: false)).ToList();
					break;
				case Key.PageUp:
					if (state.ControlDown)
						YScrollValue -= YScrollViewportFloor / 2;
					else
					{
						var savedYScrollViewportFloor = YScrollViewportFloor;
						Selections = Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, 1 - savedYScrollViewportFloor, 0, state.ShiftDown)).ToList();
					}
					break;
				case Key.PageDown:
					if (state.ControlDown)
						YScrollValue += YScrollViewportFloor / 2;
					else
					{
						var savedYScrollViewportFloor = YScrollViewportFloor;
						Selections = Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, savedYScrollViewportFloor - 1, 0, state.ShiftDown)).ToList();
					}
					break;
				case Key.Tab:
					{
						if (Selections.AsParallel().All(range => (!range.HasSelection) || (TextView.GetPositionLine(range.Start) == TextView.GetPositionLine(Math.Max(range.Start, range.End - 1)))))
						{
							if (!state.ShiftDown)
								Execute_Internal_Text();
							else
							{
								var tabs = Selections.AsParallel().AsOrdered().Where(range => (range.Start != 0) && (Text.GetString(range.Start - 1, 1) == "\t")).Select(range => Range.FromIndex(range.Start - 1, 1)).ToList();
								Replace(tabs, null);
							}
							break;
						}

						var selLines = Selections.AsParallel().AsOrdered().Where(a => a.HasSelection).Select(range => new { start = TextView.GetPositionLine(range.Start), end = TextView.GetPositionLine(range.End - 1) }).ToList();
						var lines = selLines.SelectMany(entry => Enumerable.Range(entry.start, entry.end - entry.start + 1)).Distinct().OrderBy().ToDictionary(line => line, line => TextView.GetPosition(line, 0));
						int length;
						string replace;
						if (state.ShiftDown)
						{
							length = 1;
							replace = "";
							lines = lines.Where(entry => (TextView.GetLineLength(entry.Key) != 0) && (Text[TextView.GetPosition(entry.Key, 0)] == '\t')).ToDictionary(entry => entry.Key, entry => entry.Value);
						}
						else
						{
							length = 0;
							replace = "\t";
							lines = lines.Where(entry => TextView.GetLineLength(entry.Key) != 0).ToDictionary(entry => entry.Key, entry => entry.Value);
						}

						var sels = lines.Select(line => Range.FromIndex(line.Value, length)).ToList();
						var insert = sels.Select(range => replace).ToList();
						Replace(sels, insert);
					}
					break;
				case Key.Enter:
					Execute_Internal_Text();
					break;
				default: ret = false; break;
			}

			return ret;
		}

		void Execute_Internal_Text() => ReplaceSelections(state.ConfigureExecuteData as string, false, tryJoinUndo: true);
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

		List<string> GetSelectionStrings() => Selections.AsParallel().AsOrdered().Select(range => Text.GetString(range)).ToList();

		void EnsureVisible(bool a = true, bool b = true) { }

		NEVariables GetVariables()
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

			results.Add(NEVariable.Constant("xmin", "Selection numeric min", () => Selections.AsParallel().Select(range => Text.GetString(range)).Distinct().Select(str => double.Parse(str)).DefaultIfEmpty(0).Min()));
			results.Add(NEVariable.Constant("xmax", "Selection numeric max", () => Selections.AsParallel().Select(range => Text.GetString(range)).Distinct().Select(str => double.Parse(str)).DefaultIfEmpty(0).Max()));

			results.Add(NEVariable.Constant("xtmin", "Selection text min", () => Selections.AsParallel().Select(range => Text.GetString(range)).DefaultIfEmpty("").OrderBy(Helpers.SmartComparer(false)).First()));
			results.Add(NEVariable.Constant("xtmax", "Selection text max", () => Selections.AsParallel().Select(range => Text.GetString(range)).DefaultIfEmpty("").OrderBy(Helpers.SmartComparer(false)).Last()));

			for (var region = 1; region <= 9; ++region)
			{
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

			var lineStarts = default(List<int>);
			var initializeLineStarts = new NEVariableInitializer(() => lineStarts = Selections.AsParallel().AsOrdered().Select(range => TextView.GetPositionLine(range.Start) + 1).ToList());
			results.Add(NEVariable.List("line", "Selection line start", () => lineStarts, initializeLineStarts));
			var lineEnds = default(List<int>);
			var initializeLineEnds = new NEVariableInitializer(() => lineEnds = Selections.AsParallel().AsOrdered().Select(range => TextView.GetPositionLine(range.End) + 1).ToList());
			results.Add(NEVariable.List("lineend", "Selection line end", () => lineEnds, initializeLineEnds));

			var colStarts = default(List<int>);
			var initializeColStarts = new NEVariableInitializer(() => colStarts = Selections.AsParallel().AsOrdered().Select((range, index) => TextView.GetPositionIndex(range.Start, lineStarts[index] - 1) + 1).ToList(), initializeLineStarts);
			results.Add(NEVariable.List("col", "Selection column start", () => colStarts, initializeColStarts));
			var colEnds = default(List<int>);
			var initializeColEnds = new NEVariableInitializer(() => colEnds = Selections.AsParallel().AsOrdered().Select((range, index) => TextView.GetPositionIndex(range.End, lineEnds[index] - 1) + 1).ToList(), initializeLineEnds);
			results.Add(NEVariable.List("colend", "Selection column end", () => colEnds, initializeColEnds));

			var posStarts = default(List<int>);
			var initializePosStarts = new NEVariableInitializer(() => posStarts = Selections.Select(range => range.Start).ToList());
			results.Add(NEVariable.List("pos", "Selection position start", () => posStarts, initializePosStarts));
			var posEnds = default(List<int>);
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

			var nonNulls = default(List<Tuple<double, int>>);
			double lineStart = 0, lineIncrement = 0, geoStart = 0, geoIncrement = 0;
			var initializeNonNulls = new NEVariableInitializer(() => nonNulls = Selections.AsParallel().AsOrdered().Select((range, index) => new { str = Text.GetString(range), index }).NonNullOrWhiteSpace(obj => obj.str).Select(obj => Tuple.Create(double.Parse(obj.str), obj.index)).ToList());
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

		List<T> GetExpressionResults<T>(string expression, int? count = null) => new NEExpression(expression).EvaluateList<T>(GetVariables(), count);

		public List<Range> GetEnclosingRegions(int useRegion, bool useAllRegions = false, bool mustBeInRegion = true)
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
		public bool StringsAreFiles(List<string> strs)
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

		public bool CheckCanEncode(IEnumerable<byte[]> datas, Coder.CodePage codePage) => (datas.AsParallel().All(data => Coder.CanEncode(data, codePage))) || (ConfirmContinueWhenCannotEncode());

		public bool CheckCanEncode(IEnumerable<string> strs, Coder.CodePage codePage) => (strs.AsParallel().All(str => Coder.CanEncode(str, codePage))) || (ConfirmContinueWhenCannotEncode());

		bool ConfirmContinueWhenCannotEncode()
		{
			if (!state.SavedAnswers[nameof(ConfirmContinueWhenCannotEncode)].HasFlag(MessageOptions.All))
				state.SavedAnswers[nameof(ConfirmContinueWhenCannotEncode)] = new Message(tabsWindow)
				{
					Title = "Confirm",
					Text = "The specified encoding cannot fully represent the data. Continue anyway?",
					Options = MessageOptions.YesNoAll,
					DefaultAccept = MessageOptions.Yes,
					DefaultCancel = MessageOptions.No,
				}.Show();
			return state.SavedAnswers[nameof(ConfirmContinueWhenCannotEncode)].HasFlag(MessageOptions.Yes);
		}

		public DbConnection dbConnection { get; set; }

		public void OpenTable(Table table, string name = null)
		{
			var contentType = ContentType.IsTableType() ? ContentType : ParserType.Columns;
			var textEditor = new TextEditor(bytes: Coder.StringToBytes(table.ToString("\r\n", contentType), Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, modified: false);
			tabsWindow.AddTextEditor(textEditor);
			textEditor.ContentType = contentType;
			textEditor.DisplayName = name;
		}

		public List<string> RelativeSelectedFiles()
		{
			var fileName = FileName;
			return Selections.AsParallel().AsOrdered().Select(range => fileName.RelativeChild(Text.GetString(range))).ToList();
		}

		bool VerifyCanEncode()
		{
			if (Text.CanEncode(CodePage))
				return true;

			if (!state.SavedAnswers[nameof(VerifyCanEncode)].HasFlag(MessageOptions.All))
				state.SavedAnswers[nameof(VerifyCanEncode)] = new Message(tabsWindow)
				{
					Title = "Confirm",
					Text = "The current encoding cannot fully represent this data. Switch to UTF-8?",
					Options = MessageOptions.YesNoAllCancel,
					DefaultAccept = MessageOptions.Yes,
					DefaultCancel = MessageOptions.Cancel,
				}.Show();
			if (state.SavedAnswers[nameof(VerifyCanEncode)].HasFlag(MessageOptions.Yes))
			{
				CodePage = Coder.CodePage.UTF8;
				return true;
			}
			if (state.SavedAnswers[nameof(VerifyCanEncode)].HasFlag(MessageOptions.No))
				return true;
			throw new Exception("Invalid response");
		}

		public void Save(string fileName, bool copyOnly = false)
		{
			if ((Coder.IsStr(CodePage)) && ((Text.Length >> 20) < 50) && (!VerifyCanEncode()))
				return;

			var triedReadOnly = false;
			while (true)
			{
				try
				{
					//if ((!copyOnly) && (watcher != null))
					//	watcher.EnableRaisingEvents = false;
					File.WriteAllBytes(fileName, FileSaver.Encrypt(FileSaver.Compress(Text.GetBytes(CodePage), Compressed), AESKey));
					//if ((!copyOnly) && (watcher != null))
					//	watcher.EnableRaisingEvents = true;
					break;
				}
				catch (UnauthorizedAccessException)
				{
					if ((triedReadOnly) || (!new FileInfo(fileName).IsReadOnly))
						throw;

					if (!state.SavedAnswers[nameof(Save)].HasFlag(MessageOptions.All))
						state.SavedAnswers[nameof(Save)] = new Message(tabsWindow)
						{
							Title = "Confirm",
							Text = "Save failed. Remove read-only flag?",
							Options = MessageOptions.YesNoAll,
							DefaultAccept = MessageOptions.Yes,
							DefaultCancel = MessageOptions.No,
						}.Show();
					if (!state.SavedAnswers[nameof(Save)].HasFlag(MessageOptions.Yes))
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

		public void SetFileName(string fileName)
		{
			if (FileName == fileName)
				return;

			FileName = fileName;
			ContentType = ParserExtensions.GetParserType(FileName);
			DisplayName = null;

			// TODO SetAutoRefresh();
		}

		public bool CanClose()
		{
			if (!IsModified)
				return true;

			if (!state.SavedAnswers[nameof(CanClose)].HasFlag(MessageOptions.All))
				state.SavedAnswers[nameof(CanClose)] = new Message(tabsWindow)
				{
					Title = "Confirm",
					Text = "Do you want to save changes?",
					Options = MessageOptions.YesNoAllCancel,
					DefaultCancel = MessageOptions.Cancel,
				}.Show(false);

			if (state.SavedAnswers[nameof(CanClose)].HasFlag(MessageOptions.No))
				return true;
			if (state.SavedAnswers[nameof(CanClose)].HasFlag(MessageOptions.Yes))
			{
				Execute_File_Save_Save();
				return !IsModified;
			}
			return false;
		}

		public DateTime fileLastWrite { get; set; }

		public void SetAutoRefresh(bool? value = null)
		{
			//TODO
			//ClearWatcher();

			//if (value.HasValue)
			//	AutoRefresh = value.Value;
			//if ((!AutoRefresh) || (!File.Exists(FileName)))
			//	return;

			//watcher = new FileSystemWatcher
			//{
			//	Path = Path.GetDirectoryName(FileName),
			//	NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime,
			//	Filter = Path.GetFileName(FileName),
			//};
			//watcher.Changed += (s1, e1) =>
			//{
			//	watcherFileModified = true;
			//	Dispatcher.Invoke(() => TabsParent.QueueDoActivated());
			//};
			//watcher.EnableRaisingEvents = true;
		}

		public void OpenFile(string fileName, string displayName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, ParserType contentType = ParserType.None, bool? modified = null, bool keepUndo = false)
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

			FileSaver.HandleDecrypt(tabsWindow, ref bytes, out var aesKey);
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
				UndoRedo.Clear(ref newUndoRedo);
			SetModifiedFlag(isModified);
		}

		public void Goto(int? line, int? column, int? index)
		{
			var pos = 0;
			if (line.HasValue)
			{
				var useLine = Math.Max(0, Math.Min(line.Value, TextView.NumLines) - 1);
				int useIndex;
				if (column.HasValue)
					useIndex = GetIndexFromColumn(useLine, Math.Max(0, column.Value - 1), true);
				else if (index.HasValue)
					useIndex = Math.Max(0, Math.Min(index.Value - 1, TextView.GetLineLength(useLine)));
				else
					useIndex = 0;

				pos = TextView.GetPosition(useLine, useIndex);
			}
			Selections = new List<Range> { new Range(pos) };
		}

		public List<string> DragFiles { get; set; }

		string savedBitmapText;
		System.Drawing.Bitmap savedBitmap;
		private ShutdownData shutdownData;

		public System.Drawing.Bitmap GetBitmap()
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
			//DiffTarget = null;
			//ClearWatcher();
			shutdownData?.OnShutdown();
		}
	}
}
