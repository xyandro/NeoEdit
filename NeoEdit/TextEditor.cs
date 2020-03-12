using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Dialogs;
using NeoEdit.Program.Expressions;
using NeoEdit.Program.Parsing;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program
{
	public partial class TextEditor
	{
		const int tabStop = 4;
		AnswerResult savedAnswers => TabsParent.savedAnswers;

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

		public void ReplaceSelections(string str, bool highlight = true, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false) => ReplaceSelections(Enumerable.Repeat(str, Selections.Count).ToList(), highlight, replaceType, tryJoinUndo);

		public void ReplaceSelections(List<string> strs, bool highlight = true, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false)
		{
			Replace(Selections, strs, replaceType, tryJoinUndo);

			if (highlight)
				Selections = Selections.AsParallel().AsOrdered().Select((range, index) => new Range(range.End, range.End - (strs == null ? 0 : strs[index].Length))).ToList();
			else
				Selections = Selections.AsParallel().AsOrdered().Select(range => new Range(range.End)).ToList();
		}

		public void Replace(IReadOnlyList<Range> ranges, List<string> strs, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false)
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

		public void PreHandleCommand(CommandState state)
		{
			switch (state.Command)
			{
				case NECommand.Edit_Paste_Paste: Command_Pre_Edit_Paste_Paste(ref state.PreHandleData); break;
				case NECommand.Edit_Paste_RotatePaste: Command_Pre_Edit_Paste_Paste(ref state.PreHandleData); break;
				case NECommand.Select_RepeatsCaseSensitive_Tabs_Match: Command_Pre_Select_Repeats_Tabs_MatchMismatch(ref state.PreHandleData, true); break;
				case NECommand.Select_RepeatsCaseSensitive_Tabs_Mismatch: Command_Pre_Select_Repeats_Tabs_MatchMismatch(ref state.PreHandleData, true); break;
				case NECommand.Select_RepeatsCaseInsensitive_Tabs_Match: Command_Pre_Select_Repeats_Tabs_MatchMismatch(ref state.PreHandleData, false); break;
				case NECommand.Select_RepeatsCaseInsensitive_Tabs_Mismatch: Command_Pre_Select_Repeats_Tabs_MatchMismatch(ref state.PreHandleData, false); break;
				case NECommand.Select_RepeatsCaseSensitive_Tabs_Common: Command_Pre_Select_Repeats_Tabs_CommonNonCommon(ref state.PreHandleData, true); break;
				case NECommand.Select_RepeatsCaseSensitive_Tabs_NonCommon: Command_Pre_Select_Repeats_Tabs_CommonNonCommon(ref state.PreHandleData, true); break;
				case NECommand.Select_RepeatsCaseInsensitive_Tabs_Common: Command_Pre_Select_Repeats_Tabs_CommonNonCommon(ref state.PreHandleData, false); break;
				case NECommand.Select_RepeatsCaseInsensitive_Tabs_NonCommon: Command_Pre_Select_Repeats_Tabs_CommonNonCommon(ref state.PreHandleData, false); break;
				case NECommand.Select_Selection_ToggleAnchor: Command_Pre_Select_Selection_ToggleAnchor(ref state.PreHandleData); break;
			}
		}

		public void GetCommandParameters(CommandState state, TabsWindow TabsParent)
		{
			switch (state.Command)
			{
				case NECommand.File_Save_SaveAsByExpression: state.Parameters = Command_File_SaveCopy_SaveCopyByExpression_Dialog(); break;
				case NECommand.File_Copy_CopyToByExpression: state.Parameters = Command_File_SaveCopy_SaveCopyByExpression_Dialog(); break;
				case NECommand.File_Operations_RenameByExpression: state.Parameters = Command_File_Operations_RenameByExpression_Dialog(); break;
				case NECommand.File_Operations_SetDisplayName: state.Parameters = Command_File_SaveCopy_SaveCopyByExpression_Dialog(); break;
				case NECommand.File_Encoding_Encoding: state.Parameters = Command_File_Encoding_Encoding_Dialog(); break;
				case NECommand.File_Encoding_ReopenWithEncoding: state.Parameters = Command_File_Encoding_ReopenWithEncoding_Dialog(); break;
				case NECommand.File_Encoding_LineEndings: state.Parameters = Command_File_Encoding_LineEndings_Dialog(); break;
				case NECommand.File_Encrypt: state.Parameters = Command_File_Encrypt_Dialog(state.MultiStatus); break;
				case NECommand.Edit_Find_Find: state.Parameters = Command_Edit_Find_Find_Dialog(); break;
				case NECommand.Edit_Find_RegexReplace: state.Parameters = Command_Edit_Find_RegexReplace_Dialog(); break;
				case NECommand.Edit_Rotate: state.Parameters = Command_Edit_Rotate_Dialog(); break;
				case NECommand.Edit_Repeat: state.Parameters = Command_Edit_Repeat_Dialog(); break;
				case NECommand.Edit_Data_Hash: state.Parameters = Command_Edit_Data_Hash_Dialog(); break;
				case NECommand.Edit_Data_Compress: state.Parameters = Command_Edit_Data_Compress_Dialog(); break;
				case NECommand.Edit_Data_Decompress: state.Parameters = Command_Edit_Data_Decompress_Dialog(); break;
				case NECommand.Edit_Data_Encrypt: state.Parameters = Command_Edit_Data_Encrypt_Dialog(); break;
				case NECommand.Edit_Data_Decrypt: state.Parameters = Command_Edit_Data_Decrypt_Dialog(); break;
				case NECommand.Edit_Data_Sign: state.Parameters = Command_Edit_Data_Sign_Dialog(); break;
				case NECommand.Edit_Sort: state.Parameters = Command_Edit_Sort_Dialog(); break;
				case NECommand.Edit_Convert: state.Parameters = Command_Edit_Convert_Dialog(); break;
				case NECommand.Edit_ModifyRegions: state.Parameters = Command_Edit_ModifyRegions_Dialog(); break;
				//TODO
				//case NECommand.Diff_IgnoreCharacters: state.Parameters = Command_Diff_IgnoreCharacters_Dialog(); break;
				//case NECommand.Diff_Fix_Whitespace: state.Parameters = Command_Diff_Fix_Whitespace_Dialog(); break;
				case NECommand.Files_Name_MakeAbsolute: state.Parameters = Command_Files_Name_MakeAbsolute_Dialog(); break;
				case NECommand.Files_Name_MakeRelative: state.Parameters = Command_Files_Name_MakeRelative_Dialog(); break;
				case NECommand.Files_Name_GetUnique: state.Parameters = Command_Files_Name_GetUnique_Dialog(); break;
				case NECommand.Files_Set_Size: state.Parameters = Command_Files_Set_Size_Dialog(); break;
				case NECommand.Files_Set_Time_Write: state.Parameters = Command_Files_Set_Time_Dialog(); break;
				case NECommand.Files_Set_Time_Access: state.Parameters = Command_Files_Set_Time_Dialog(); break;
				case NECommand.Files_Set_Time_Create: state.Parameters = Command_Files_Set_Time_Dialog(); break;
				case NECommand.Files_Set_Time_All: state.Parameters = Command_Files_Set_Time_Dialog(); break;
				case NECommand.Files_Set_Attributes: state.Parameters = Command_Files_Set_Attributes_Dialog(); break;
				case NECommand.Files_Find: state.Parameters = Command_Files_Find_Dialog(); break;
				case NECommand.Files_Insert: state.Parameters = Command_Files_Insert_Dialog(); break;
				case NECommand.Files_Create_FromExpressions: state.Parameters = Command_Files_Create_FromExpressions_Dialog(); break;
				case NECommand.Files_Select_ByVersionControlStatus: state.Parameters = Command_Files_Select_ByVersionControlStatus_Dialog(); break;
				case NECommand.Files_Hash: state.Parameters = Command_Files_Hash_Dialog(); break;
				case NECommand.Files_Sign: state.Parameters = Command_Files_Sign_Dialog(); break;
				case NECommand.Files_Operations_Copy: state.Parameters = Command_Files_Operations_CopyMove_Dialog(false); break;
				case NECommand.Files_Operations_Move: state.Parameters = Command_Files_Operations_CopyMove_Dialog(true); break;
				case NECommand.Files_Operations_Encoding: state.Parameters = Command_Files_Operations_Encoding_Dialog(); break;
				case NECommand.Files_Operations_SplitFile: state.Parameters = Command_Files_Operations_SplitFile_Dialog(); break;
				case NECommand.Files_Operations_CombineFiles: state.Parameters = Command_Files_Operations_CombineFiles_Dialog(); break;
				case NECommand.Edit_Expression_Expression: state.Parameters = Command_Edit_Expression_Expression_Dialog(); break;
				case NECommand.Text_Select_Trim: state.Parameters = Command_Text_Select_Trim_Dialog(); break;
				case NECommand.Text_Select_ByWidth: state.Parameters = Command_Text_Select_ByWidth_Dialog(); break;
				case NECommand.Text_Select_WholeWord: state.Parameters = Command_Text_Select_WholeBoundedWord_Dialog(true); break;
				case NECommand.Text_Select_BoundedWord: state.Parameters = Command_Text_Select_WholeBoundedWord_Dialog(false); break;
				case NECommand.Text_Width: state.Parameters = Command_Text_Width_Dialog(); break;
				case NECommand.Text_Trim: state.Parameters = Command_Text_Trim_Dialog(); break;
				case NECommand.Text_Unicode: state.Parameters = Command_Text_Unicode_Dialog(); break;
				case NECommand.Text_RandomText: state.Parameters = Command_Text_RandomText_Dialog(); break;
				case NECommand.Text_ReverseRegEx: state.Parameters = Command_Text_ReverseRegEx_Dialog(); break;
				case NECommand.Text_FirstDistinct: state.Parameters = Command_Text_FirstDistinct_Dialog(); break;
				case NECommand.Numeric_ConvertBase: state.Parameters = Command_Numeric_ConvertBase_Dialog(); break;
				case NECommand.Numeric_Series_Linear: state.Parameters = Command_Numeric_Series_LinearGeometric_Dialog(true); break;
				case NECommand.Numeric_Series_Geometric: state.Parameters = Command_Numeric_Series_LinearGeometric_Dialog(false); break;
				case NECommand.Numeric_Scale: state.Parameters = Command_Numeric_Scale_Dialog(); break;
				case NECommand.Numeric_Floor: state.Parameters = Command_Numeric_Floor_Dialog(); break;
				case NECommand.Numeric_Ceiling: state.Parameters = Command_Numeric_Ceiling_Dialog(); break;
				case NECommand.Numeric_Round: state.Parameters = Command_Numeric_Round_Dialog(); break;
				case NECommand.Numeric_Limit: state.Parameters = Command_Numeric_Limit_Dialog(); break;
				case NECommand.Numeric_Cycle: state.Parameters = Command_Numeric_Cycle_Dialog(); break;
				case NECommand.Numeric_RandomNumber: state.Parameters = Command_Numeric_RandomNumber_Dialog(); break;
				case NECommand.Numeric_CombinationsPermutations: state.Parameters = Command_Numeric_CombinationsPermutations_Dialog(); break;
				case NECommand.Numeric_MinMaxValues: state.Parameters = Command_Numeric_MinMaxValues_Dialog(); break;
				case NECommand.DateTime_Format: state.Parameters = Command_DateTime_Format_Dialog(); break;
				case NECommand.DateTime_ToTimeZone: state.Parameters = Command_DateTime_ToTimeZone_Dialog(); break;
				case NECommand.Image_GrabColor: state.Parameters = Command_Image_GrabColor_Dialog(); break;
				case NECommand.Image_GrabImage: state.Parameters = Command_Image_GrabImage_Dialog(); break;
				case NECommand.Image_AdjustColor: state.Parameters = Command_Image_AdjustColor_Dialog(); break;
				case NECommand.Image_AddColor: state.Parameters = Command_Image_AddOverlayColor_Dialog(true); break;
				case NECommand.Image_OverlayColor: state.Parameters = Command_Image_AddOverlayColor_Dialog(false); break;
				case NECommand.Image_Size: state.Parameters = Command_Image_Size_Dialog(); break;
				case NECommand.Image_Crop: state.Parameters = Command_Image_Crop_Dialog(); break;
				case NECommand.Image_Rotate: state.Parameters = Command_Image_Rotate_Dialog(); break;
				case NECommand.Image_GIF_Animate: state.Parameters = Command_Image_GIF_Animate_Dialog(); break;
				case NECommand.Image_GIF_Split: state.Parameters = Command_Image_GIF_Split_Dialog(); break;
				case NECommand.Table_Convert: state.Parameters = Command_Table_Convert_Dialog(); break;
				case NECommand.Table_TextToTable: state.Parameters = Command_Table_TextToTable_Dialog(); break;
				case NECommand.Table_EditTable: state.Parameters = Command_Table_EditTable_Dialog(); break;
				case NECommand.Table_AddColumn: state.Parameters = Command_Table_AddColumn_Dialog(); break;
				case NECommand.Table_Select_RowsByExpression: state.Parameters = Command_Table_Select_RowsByExpression_Dialog(); break;
				case NECommand.Table_Join: state.Parameters = Command_Table_Join_Dialog(); break;
				case NECommand.Table_Database_GenerateInserts: state.Parameters = Command_Table_Database_GenerateInserts_Dialog(); break;
				case NECommand.Table_Database_GenerateUpdates: state.Parameters = Command_Table_Database_GenerateUpdates_Dialog(); break;
				case NECommand.Table_Database_GenerateDeletes: state.Parameters = Command_Table_Database_GenerateDeletes_Dialog(); break;
				case NECommand.Position_Goto_Lines: state.Parameters = Command_Position_Goto_Dialog(GotoType.Line); break;
				case NECommand.Position_Goto_Columns: state.Parameters = Command_Position_Goto_Dialog(GotoType.Column); break;
				case NECommand.Position_Goto_Indexes: state.Parameters = Command_Position_Goto_Dialog(GotoType.Index); break;
				case NECommand.Position_Goto_Positions: state.Parameters = Command_Position_Goto_Dialog(GotoType.Position); break;
				case NECommand.Content_Ancestor: state.Parameters = Command_Content_Ancestor_Dialog(); break;
				case NECommand.Content_Attributes: state.Parameters = Command_Content_Attributes_Dialog(); break;
				case NECommand.Content_WithAttribute: state.Parameters = Command_Content_WithAttribute_Dialog(); break;
				case NECommand.Content_Children_WithAttribute: state.Parameters = Command_Content_Children_WithAttribute_Dialog(); break;
				case NECommand.Content_Descendants_WithAttribute: state.Parameters = Command_Content_Descendants_WithAttribute_Dialog(); break;
				case NECommand.Network_AbsoluteURL: state.Parameters = Command_Network_AbsoluteURL_Dialog(); break;
				case NECommand.Network_FetchFile: state.Parameters = Command_Network_FetchFile_Dialog(); break;
				case NECommand.Network_FetchStream: state.Parameters = Command_Network_FetchStream_Dialog(); break;
				case NECommand.Network_FetchPlaylist: state.Parameters = Command_Network_FetchPlaylist_Dialog(); break;
				case NECommand.Network_Ping: state.Parameters = Command_Network_Ping_Dialog(); break;
				case NECommand.Network_ScanPorts: state.Parameters = Command_Network_ScanPorts_Dialog(); break;
				case NECommand.Network_WCF_GetConfig: state.Parameters = Command_Network_WCF_GetConfig_Dialog(); break;
				case NECommand.Network_WCF_InterceptCalls: state.Parameters = Command_Network_WCF_InterceptCalls_Dialog(); break;
				case NECommand.Database_Connect: state.Parameters = Command_Database_Connect_Dialog(); break;
				case NECommand.Database_Examine: Command_Database_Examine_Dialog(); break;
				case NECommand.Select_Limit: state.Parameters = Command_Select_Limit_Dialog(); break;
				case NECommand.Select_RepeatsCaseSensitive_ByCount: state.Parameters = Command_Select_Repeats_ByCount_Dialog(); break;
				case NECommand.Select_RepeatsCaseInsensitive_ByCount: state.Parameters = Command_Select_Repeats_ByCount_Dialog(); break;
				case NECommand.Select_Split: state.Parameters = Command_Select_Split_Dialog(); break;
			}
		}

		public void ExecuteCommand(CommandState state)
		{
			switch (state.Command)
			{
				case NECommand.File_New_FromSelections: Command_File_New_FromSelections(); break;
				case NECommand.File_Open_Selected: Command_File_Open_Selected(); break;
				case NECommand.File_Save_Save: Command_File_Save_Save(); break;
				case NECommand.File_Save_SaveAs: Command_File_SaveCopy_SaveCopy(); break;
				case NECommand.File_Save_SaveAsClipboard: Command_File_SaveCopy_SaveCopyClipboard(); break;
				case NECommand.File_Save_SaveAsByExpression: Command_File_SaveCopy_SaveCopyByExpression(state.Parameters as GetExpressionDialog.Result); break;
				case NECommand.File_Copy_CopyTo: Command_File_SaveCopy_SaveCopy(true); break;
				case NECommand.File_Copy_CopyToClipboard: Command_File_SaveCopy_SaveCopyClipboard(true); break;
				case NECommand.File_Copy_CopyToByExpression: Command_File_SaveCopy_SaveCopyByExpression(state.Parameters as GetExpressionDialog.Result, true); break;
				case NECommand.File_Copy_Path: Command_File_Copy_Path(); break;
				case NECommand.File_Copy_Name: Command_File_Copy_Name(); break;
				case NECommand.File_Copy_DisplayName: Command_File_Copy_DisplayName(); break;
				case NECommand.File_Operations_Rename: Command_File_Operations_Rename(); break;
				case NECommand.File_Operations_RenameClipboard: Command_File_Operations_RenameClipboard(); break;
				case NECommand.File_Operations_RenameByExpression: Command_File_Operations_RenameByExpression(state.Parameters as GetExpressionDialog.Result); break;
				case NECommand.File_Operations_Delete: Command_File_Operations_Delete(); break;
				case NECommand.File_Operations_Explore: Command_File_Operations_Explore(); break;
				case NECommand.File_Operations_CommandPrompt: Command_File_Operations_CommandPrompt(); break;
				case NECommand.File_Operations_VCSDiff: Command_File_Operations_VCSDiff(); break;
				case NECommand.File_Operations_SetDisplayName: Command_File_Operations_SetDisplayName(state.Parameters as GetExpressionDialog.Result); break;
				case NECommand.File_Close: Command_File_Close(); break;
				case NECommand.File_Refresh: Command_File_Refresh(); break;
				case NECommand.File_AutoRefresh: Command_File_AutoRefresh(state.MultiStatus); break;
				case NECommand.File_Revert: Command_File_Revert(); break;
				case NECommand.File_Insert_Files: Command_File_Insert_Files(); break;
				case NECommand.File_Insert_CopiedCut: Command_File_Insert_CopiedCut(); break;
				case NECommand.File_Insert_Selected: Command_File_Insert_Selected(); break;
				case NECommand.File_Encoding_Encoding: Command_File_Encoding_Encoding(state.Parameters as EncodingDialog.Result); break;
				case NECommand.File_Encoding_ReopenWithEncoding: Command_File_Encoding_ReopenWithEncoding(state.Parameters as EncodingDialog.Result); break;
				case NECommand.File_Encoding_LineEndings: Command_File_Encoding_LineEndings(state.Parameters as FileEncodingLineEndingsDialog.Result); break;
				case NECommand.File_Encrypt: Command_File_Encrypt(state.Parameters as string); break;
				case NECommand.File_Compress: Command_File_Compress(state.MultiStatus); break;
				case NECommand.Edit_Undo: Command_Edit_Undo(); break;
				case NECommand.Edit_Redo: Command_Edit_Redo(); break;
				case NECommand.Edit_Copy_Copy: Command_Edit_Copy_CutCopy(false); break;
				case NECommand.Edit_Copy_Cut: Command_Edit_Copy_CutCopy(true); break;
				case NECommand.Edit_Paste_Paste: Command_Edit_Paste_Paste(state.ShiftDown, false, state.PreHandleData); break;
				case NECommand.Edit_Paste_RotatePaste: Command_Edit_Paste_Paste(true, true, state.PreHandleData); break;
				case NECommand.Edit_Find_Find: Command_Edit_Find_Find(state.Parameters as EditFindFindDialog.Result); break;
				case NECommand.Edit_Find_RegexReplace: Command_Edit_Find_RegexReplace(state.Parameters as EditFindRegexReplaceDialog.Result); break;
				case NECommand.Edit_CopyDown: Command_Edit_CopyDown(); break;
				case NECommand.Edit_Rotate: Command_Edit_Rotate(state.Parameters as EditRotateDialog.Result); break;
				case NECommand.Edit_Repeat: Command_Edit_Repeat(state.Parameters as EditRepeatDialog.Result); break;
				case NECommand.Edit_Escape_Markup: Command_Edit_Escape_Markup(); break;
				case NECommand.Edit_Escape_RegEx: Command_Edit_Escape_RegEx(); break;
				case NECommand.Edit_Escape_URL: Command_Edit_Escape_URL(); break;
				case NECommand.Edit_Unescape_Markup: Command_Edit_Unescape_Markup(); break;
				case NECommand.Edit_Unescape_RegEx: Command_Edit_Unescape_RegEx(); break;
				case NECommand.Edit_Unescape_URL: Command_Edit_Unescape_URL(); break;
				case NECommand.Edit_Data_Hash: Command_Edit_Data_Hash(state.Parameters as EditDataHashDialog.Result); break;
				case NECommand.Edit_Data_Compress: Command_Edit_Data_Compress(state.Parameters as EditDataCompressDialog.Result); break;
				case NECommand.Edit_Data_Decompress: Command_Edit_Data_Decompress(state.Parameters as EditDataCompressDialog.Result); break;
				case NECommand.Edit_Data_Encrypt: Command_Edit_Data_Encrypt(state.Parameters as EditDataEncryptDialog.Result); break;
				case NECommand.Edit_Data_Decrypt: Command_Edit_Data_Decrypt(state.Parameters as EditDataEncryptDialog.Result); break;
				case NECommand.Edit_Data_Sign: Command_Edit_Data_Sign(state.Parameters as EditDataSignDialog.Result); break;
				case NECommand.Edit_Sort: Command_Edit_Sort(state.Parameters as EditSortDialog.Result); break;
				case NECommand.Edit_Convert: Command_Edit_Convert(state.Parameters as EditConvertDialog.Result); break;
				case NECommand.Edit_ModifyRegions: Command_Edit_ModifyRegions(state.Parameters as EditModifyRegionsDialog.Result); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region1: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Select, 1); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region2: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Select, 2); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region3: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Select, 3); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region4: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Select, 4); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region5: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Select, 5); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region6: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Select, 6); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region7: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Select, 7); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region8: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Select, 8); break;
				case NECommand.Edit_ModifyRegions_Select_Select_Region9: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Select, 9); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region1: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Previous, 1); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region2: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Previous, 2); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region3: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Previous, 3); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region4: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Previous, 4); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region5: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Previous, 5); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region6: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Previous, 6); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region7: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Previous, 7); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region8: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Previous, 8); break;
				case NECommand.Edit_ModifyRegions_Select_Previous_Region9: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Previous, 9); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region1: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Next, 1); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region2: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Next, 2); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region3: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Next, 3); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region4: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Next, 4); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region5: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Next, 5); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region6: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Next, 6); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region7: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Next, 7); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region8: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Next, 8); break;
				case NECommand.Edit_ModifyRegions_Select_Next_Region9: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Next, 9); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region1: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Enclosing, 1); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region2: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Enclosing, 2); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region3: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Enclosing, 3); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region4: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Enclosing, 4); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region5: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Enclosing, 5); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region6: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Enclosing, 6); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region7: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Enclosing, 7); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region8: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Enclosing, 8); break;
				case NECommand.Edit_ModifyRegions_Select_Enclosing_Region9: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_Enclosing, 9); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region1: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithEnclosing, 1); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region2: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithEnclosing, 2); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region3: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithEnclosing, 3); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region4: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithEnclosing, 4); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region5: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithEnclosing, 5); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region6: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithEnclosing, 6); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region7: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithEnclosing, 7); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region8: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithEnclosing, 8); break;
				case NECommand.Edit_ModifyRegions_Select_WithEnclosing_Region9: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithEnclosing, 9); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region1: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithoutEnclosing, 1); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region2: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithoutEnclosing, 2); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region3: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithoutEnclosing, 3); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region4: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithoutEnclosing, 4); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region5: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithoutEnclosing, 5); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region6: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithoutEnclosing, 6); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region7: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithoutEnclosing, 7); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region8: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithoutEnclosing, 8); break;
				case NECommand.Edit_ModifyRegions_Select_WithoutEnclosing_Region9: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Select_WithoutEnclosing, 9); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region1: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Set, 1); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region2: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Set, 2); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region3: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Set, 3); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region4: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Set, 4); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region5: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Set, 5); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region6: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Set, 6); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region7: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Set, 7); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region8: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Set, 8); break;
				case NECommand.Edit_ModifyRegions_Modify_Set_Region9: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Set, 9); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region1: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Clear, 1); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region2: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Clear, 2); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region3: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Clear, 3); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region4: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Clear, 4); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region5: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Clear, 5); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region6: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Clear, 6); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region7: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Clear, 7); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region8: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Clear, 8); break;
				case NECommand.Edit_ModifyRegions_Modify_Clear_Region9: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Clear, 9); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region1: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Remove, 1); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region2: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Remove, 2); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region3: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Remove, 3); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region4: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Remove, 4); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region5: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Remove, 5); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region6: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Remove, 6); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region7: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Remove, 7); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region8: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Remove, 8); break;
				case NECommand.Edit_ModifyRegions_Modify_Remove_Region9: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Remove, 9); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region1: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Add, 1); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region2: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Add, 2); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region3: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Add, 3); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region4: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Add, 4); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region5: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Add, 5); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region6: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Add, 6); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region7: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Add, 7); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region8: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Add, 8); break;
				case NECommand.Edit_ModifyRegions_Modify_Add_Region9: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Add, 9); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region1: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Unite, 1); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region2: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Unite, 2); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region3: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Unite, 3); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region4: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Unite, 4); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region5: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Unite, 5); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region6: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Unite, 6); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region7: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Unite, 7); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region8: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Unite, 8); break;
				case NECommand.Edit_ModifyRegions_Modify_Unite_Region9: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Unite, 9); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region1: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Intersect, 1); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region2: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Intersect, 2); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region3: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Intersect, 3); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region4: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Intersect, 4); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region5: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Intersect, 5); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region6: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Intersect, 6); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region7: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Intersect, 7); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region8: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Intersect, 8); break;
				case NECommand.Edit_ModifyRegions_Modify_Intersect_Region9: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Intersect, 9); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region1: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Exclude, 1); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region2: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Exclude, 2); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region3: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Exclude, 3); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region4: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Exclude, 4); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region5: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Exclude, 5); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region6: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Exclude, 6); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region7: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Exclude, 7); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region8: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Exclude, 8); break;
				case NECommand.Edit_ModifyRegions_Modify_Exclude_Region9: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Exclude, 9); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region1: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Repeat, 1); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region2: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Repeat, 2); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region3: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Repeat, 3); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region4: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Repeat, 4); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region5: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Repeat, 5); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region6: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Repeat, 6); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region7: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Repeat, 7); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region8: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Repeat, 8); break;
				case NECommand.Edit_ModifyRegions_Modify_Repeat_Region9: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Modify_Repeat, 9); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region1: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_Enclosing, 1); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region2: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_Enclosing, 2); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region3: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_Enclosing, 3); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region4: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_Enclosing, 4); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region5: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_Enclosing, 5); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region6: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_Enclosing, 6); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region7: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_Enclosing, 7); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region8: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_Enclosing, 8); break;
				case NECommand.Edit_ModifyRegions_Copy_Enclosing_Region9: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_Enclosing, 9); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region1: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_EnclosingIndex, 1); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region2: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_EnclosingIndex, 2); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region3: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_EnclosingIndex, 3); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region4: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_EnclosingIndex, 4); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region5: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_EnclosingIndex, 5); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region6: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_EnclosingIndex, 6); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region7: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_EnclosingIndex, 7); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region8: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_EnclosingIndex, 8); break;
				case NECommand.Edit_ModifyRegions_Copy_EnclosingIndex_Region9: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Copy_EnclosingIndex, 9); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region1: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Flatten, 1); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region2: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Flatten, 2); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region3: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Flatten, 3); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region4: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Flatten, 4); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region5: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Flatten, 5); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region6: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Flatten, 6); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region7: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Flatten, 7); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region8: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Flatten, 8); break;
				case NECommand.Edit_ModifyRegions_Transform_Flatten_Region9: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Flatten, 9); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region1: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Transpose, 1); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region2: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Transpose, 2); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region3: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Transpose, 3); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region4: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Transpose, 4); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region5: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Transpose, 5); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region6: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Transpose, 6); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region7: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Transpose, 7); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region8: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Transpose, 8); break;
				case NECommand.Edit_ModifyRegions_Transform_Transpose_Region9: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Transpose, 9); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region1: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateLeft, 1); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region2: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateLeft, 2); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region3: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateLeft, 3); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region4: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateLeft, 4); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region5: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateLeft, 5); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region6: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateLeft, 6); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region7: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateLeft, 7); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region8: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateLeft, 8); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateLeft_Region9: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateLeft, 9); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region1: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateRight, 1); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region2: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateRight, 2); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region3: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateRight, 3); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region4: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateRight, 4); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region5: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateRight, 5); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region6: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateRight, 6); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region7: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateRight, 7); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region8: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateRight, 8); break;
				case NECommand.Edit_ModifyRegions_Transform_RotateRight_Region9: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_RotateRight, 9); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region1: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Rotate180, 1); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region2: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Rotate180, 2); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region3: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Rotate180, 3); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region4: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Rotate180, 4); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region5: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Rotate180, 5); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region6: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Rotate180, 6); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region7: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Rotate180, 7); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region8: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Rotate180, 8); break;
				case NECommand.Edit_ModifyRegions_Transform_Rotate180_Region9: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_Rotate180, 9); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region1: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorHorizontal, 1); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region2: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorHorizontal, 2); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region3: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorHorizontal, 3); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region4: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorHorizontal, 4); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region5: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorHorizontal, 5); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region6: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorHorizontal, 6); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region7: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorHorizontal, 7); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region8: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorHorizontal, 8); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorHorizontal_Region9: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorHorizontal, 9); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region1: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorVertical, 1); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region2: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorVertical, 2); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region3: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorVertical, 3); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region4: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorVertical, 4); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region5: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorVertical, 5); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region6: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorVertical, 6); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region7: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorVertical, 7); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region8: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorVertical, 8); break;
				case NECommand.Edit_ModifyRegions_Transform_MirrorVertical_Region9: Command_Edit_ModifyRegions(EditModifyRegionsDialog.Action.Transform_MirrorVertical, 9); break;
				case NECommand.Edit_Navigate_WordLeft: Command_Edit_Navigate_WordLeftRight(false, state.ShiftDown); break;
				case NECommand.Edit_Navigate_WordRight: Command_Edit_Navigate_WordLeftRight(true, state.ShiftDown); break;
				case NECommand.Edit_Navigate_AllLeft: Command_Edit_Navigate_AllLeft(state.ShiftDown); break;
				case NECommand.Edit_Navigate_AllRight: Command_Edit_Navigate_AllRight(state.ShiftDown); break;
				case NECommand.Edit_Navigate_JumpBy_Words: Command_Edit_Navigate_JumpBy(JumpByType.Words); break;
				case NECommand.Edit_Navigate_JumpBy_Numbers: Command_Edit_Navigate_JumpBy(JumpByType.Numbers); break;
				case NECommand.Edit_Navigate_JumpBy_Paths: Command_Edit_Navigate_JumpBy(JumpByType.Paths); break;
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
				case NECommand.Files_Name_Simplify: Command_Files_Name_Simplify(); break;
				case NECommand.Files_Name_MakeAbsolute: Command_Files_Name_MakeAbsolute(state.Parameters as FilesNamesMakeAbsoluteRelativeDialog.Result); break;
				case NECommand.Files_Name_MakeRelative: Command_Files_Name_MakeRelative(state.Parameters as FilesNamesMakeAbsoluteRelativeDialog.Result); break;
				case NECommand.Files_Name_GetUnique: Command_Files_Name_GetUnique(state.Parameters as FilesNamesGetUniqueDialog.Result); break;
				case NECommand.Files_Name_Sanitize: Command_Files_Name_Sanitize(); break;
				case NECommand.Files_Get_Size: Command_Files_Get_Size(); break;
				case NECommand.Files_Get_Time_Write: Command_Files_Get_Time(TimestampType.Write); break;
				case NECommand.Files_Get_Time_Access: Command_Files_Get_Time(TimestampType.Access); break;
				case NECommand.Files_Get_Time_Create: Command_Files_Get_Time(TimestampType.Create); break;
				case NECommand.Files_Get_Attributes: Command_Files_Get_Attributes(); break;
				case NECommand.Files_Get_Version_File: Command_Files_Get_Version_File(); break;
				case NECommand.Files_Get_Version_Product: Command_Files_Get_Version_Product(); break;
				case NECommand.Files_Get_Version_Assembly: Command_Files_Get_Version_Assembly(); break;
				case NECommand.Files_Get_Children: Command_Files_Get_ChildrenDescendants(false); break;
				case NECommand.Files_Get_Descendants: Command_Files_Get_ChildrenDescendants(true); break;
				case NECommand.Files_Get_VersionControlStatus: Command_Files_Get_VersionControlStatus(); break;
				case NECommand.Files_Set_Size: Command_Files_Set_Size(state.Parameters as FilesSetSizeDialog.Result); break;
				case NECommand.Files_Set_Time_Write: Command_Files_Set_Time(TimestampType.Write, state.Parameters as FilesSetTimeDialog.Result); break;
				case NECommand.Files_Set_Time_Access: Command_Files_Set_Time(TimestampType.Access, state.Parameters as FilesSetTimeDialog.Result); break;
				case NECommand.Files_Set_Time_Create: Command_Files_Set_Time(TimestampType.Create, state.Parameters as FilesSetTimeDialog.Result); break;
				case NECommand.Files_Set_Time_All: Command_Files_Set_Time(TimestampType.All, state.Parameters as FilesSetTimeDialog.Result); break;
				case NECommand.Files_Set_Attributes: Command_Files_Set_Attributes(state.Parameters as FilesSetAttributesDialog.Result); break;
				case NECommand.Files_Find: Command_Files_Find(state.Parameters as FilesFindDialog.Result); break;
				case NECommand.Files_Insert: Command_Files_Insert(state.Parameters as FilesInsertDialog.Result); break;
				case NECommand.Files_Create_Files: Command_Files_Create_Files(); break;
				case NECommand.Files_Create_Directories: Command_Files_Create_Directories(); break;
				case NECommand.Files_Create_FromExpressions: Command_Files_Create_FromExpressions(state.Parameters as FilesCreateFromExpressionsDialog.Result); break;
				case NECommand.Files_Select_Name_Directory: Command_Files_Select_Name(GetPathType.Directory); break;
				case NECommand.Files_Select_Name_Name: Command_Files_Select_Name(GetPathType.FileName); break;
				case NECommand.Files_Select_Name_FileNamewoExtension: Command_Files_Select_Name(GetPathType.FileNameWoExtension); break;
				case NECommand.Files_Select_Name_Extension: Command_Files_Select_Name(GetPathType.Extension); break;
				case NECommand.Files_Select_Name_Next: Command_Files_Select_Name_Next(); break;
				case NECommand.Files_Select_Files: Command_Files_Select_Files(); break;
				case NECommand.Files_Select_Directories: Command_Files_Select_Directories(); break;
				case NECommand.Files_Select_Existing: Command_Files_Select_Existing(true); break;
				case NECommand.Files_Select_NonExisting: Command_Files_Select_Existing(false); break;
				case NECommand.Files_Select_Roots: Command_Files_Select_Roots(true); break;
				case NECommand.Files_Select_NonRoots: Command_Files_Select_Roots(false); break;
				case NECommand.Files_Select_MatchDepth: Command_Files_Select_MatchDepth(); break;
				case NECommand.Files_Select_CommonAncestor: Command_Files_Select_CommonAncestor(); break;
				case NECommand.Files_Select_ByVersionControlStatus: Command_Files_Select_ByVersionControlStatus(state.Parameters as FilesSelectByVersionControlStatusDialog.Result); break;
				case NECommand.Files_Hash: Command_Files_Hash(state.Parameters as FilesHashDialog.Result); break;
				case NECommand.Files_Sign: Command_Files_Sign(state.Parameters as FilesSignDialog.Result); break;
				case NECommand.Files_Operations_Copy: Command_Files_Operations_CopyMove(state.Parameters as FilesOperationsCopyMoveDialog.Result, false); break;
				case NECommand.Files_Operations_Move: Command_Files_Operations_CopyMove(state.Parameters as FilesOperationsCopyMoveDialog.Result, true); break;
				case NECommand.Files_Operations_Delete: Command_Files_Operations_Delete(); break;
				case NECommand.Files_Operations_DragDrop: Command_Files_Operations_DragDrop(); break;
				case NECommand.Files_Operations_Explore: Command_Files_Operations_Explore(); break;
				case NECommand.Files_Operations_CommandPrompt: Command_Files_Operations_CommandPrompt(); break;
				case NECommand.Files_Operations_RunCommand_Parallel: Command_Files_Operations_RunCommand_Parallel(); break;
				case NECommand.Files_Operations_RunCommand_Sequential: Command_Files_Operations_RunCommand_Sequential(); break;
				case NECommand.Files_Operations_RunCommand_Shell: Command_Files_Operations_RunCommand_Shell(); break;
				case NECommand.Files_Operations_Encoding: Command_Files_Operations_Encoding(state.Parameters as FilesOperationsEncodingDialog.Result); break;
				case NECommand.Files_Operations_SplitFile: Command_Files_Operations_SplitFile(state.Parameters as FilesOperationsSplitFileDialog.Result); break;
				case NECommand.Files_Operations_CombineFiles: Command_Files_Operations_CombineFiles(state.Parameters as FilesOperationsCombineFilesDialog.Result); break;
				case NECommand.Edit_Expression_Expression: Command_Edit_Expression_Expression(state.Parameters as EditExpressionExpressionDialog.Result); break;
				case NECommand.Edit_Expression_EvaluateSelected: Command_Edit_Expression_EvaluateSelected(); break;
				case NECommand.Text_Select_Trim: Command_Text_Select_Trim(state.Parameters as TextTrimDialog.Result); break;
				case NECommand.Text_Select_ByWidth: Command_Text_Select_ByWidth(state.Parameters as TextWidthDialog.Result); break;
				case NECommand.Text_Select_WholeWord: Command_Text_Select_WholeBoundedWord(state.Parameters as TextSelectWholeBoundedWordDialog.Result, true); break;
				case NECommand.Text_Select_BoundedWord: Command_Text_Select_WholeBoundedWord(state.Parameters as TextSelectWholeBoundedWordDialog.Result, false); break;
				case NECommand.Text_Select_Min_Text: Command_Text_Select_MinMax_Text(false); break;
				case NECommand.Text_Select_Min_Length: Command_Text_Select_MinMax_Length(false); break;
				case NECommand.Text_Select_Max_Text: Command_Text_Select_MinMax_Text(true); break;
				case NECommand.Text_Select_Max_Length: Command_Text_Select_MinMax_Length(true); break;
				case NECommand.Text_Case_Upper: Command_Text_Case_Upper(); break;
				case NECommand.Text_Case_Lower: Command_Text_Case_Lower(); break;
				case NECommand.Text_Case_Proper: Command_Text_Case_Proper(); break;
				case NECommand.Text_Case_Toggle: Command_Text_Case_Toggle(); break;
				case NECommand.Text_Length: Command_Text_Length(); break;
				case NECommand.Text_Width: Command_Text_Width(state.Parameters as TextWidthDialog.Result); break;
				case NECommand.Text_Trim: Command_Text_Trim(state.Parameters as TextTrimDialog.Result); break;
				case NECommand.Text_SingleLine: Command_Text_SingleLine(); break;
				case NECommand.Text_Unicode: Command_Text_Unicode(state.Parameters as TextUnicodeDialog.Result); break;
				case NECommand.Text_GUID: Command_Text_GUID(); break;
				case NECommand.Text_RandomText: Command_Text_RandomText(state.Parameters as TextRandomTextDialog.Result); break;
				case NECommand.Text_LoremIpsum: Command_Text_LoremIpsum(); break;
				case NECommand.Text_ReverseRegEx: Command_Text_ReverseRegEx(state.Parameters as TextReverseRegExDialog.Result); break;
				case NECommand.Text_FirstDistinct: Command_Text_FirstDistinct(state.Parameters as TextFirstDistinctDialog.Result); break;
				case NECommand.Text_RepeatCount: Command_Text_RepeatCount(); break;
				case NECommand.Text_RepeatIndex: Command_Text_RepeatIndex(); break;
				case NECommand.Numeric_Select_Min: Command_Numeric_Select_MinMax(false); break;
				case NECommand.Numeric_Select_Max: Command_Numeric_Select_MinMax(true); break;
				case NECommand.Numeric_Select_Fraction_Whole: Command_Numeric_Select_Fraction_Whole(); break;
				case NECommand.Numeric_Select_Fraction_Fraction: Command_Numeric_Select_Fraction_Fraction(); break;
				case NECommand.Numeric_Hex_ToHex: Command_Numeric_Hex_ToHex(); break;
				case NECommand.Numeric_Hex_FromHex: Command_Numeric_Hex_FromHex(); break;
				case NECommand.Numeric_ConvertBase: Command_Numeric_ConvertBase(state.Parameters as NumericConvertBaseDialog.Result); break;
				case NECommand.Numeric_Series_ZeroBased: Command_Numeric_Series_ZeroBased(); break;
				case NECommand.Numeric_Series_OneBased: Command_Numeric_Series_OneBased(); break;
				case NECommand.Numeric_Series_Linear: Command_Numeric_Series_LinearGeometric(state.Parameters as NumericSeriesDialog.Result, true); break;
				case NECommand.Numeric_Series_Geometric: Command_Numeric_Series_LinearGeometric(state.Parameters as NumericSeriesDialog.Result, false); break;
				case NECommand.Numeric_Scale: Command_Numeric_Scale(state.Parameters as NumericScaleDialog.Result); break;
				case NECommand.Numeric_Add_Sum: Command_Numeric_Add_Sum(); break;
				case NECommand.Numeric_Add_ForwardSum: Command_Numeric_Add_ForwardReverseSum(true, false); break;
				case NECommand.Numeric_Add_ReverseSum: Command_Numeric_Add_ForwardReverseSum(false, false); break;
				case NECommand.Numeric_Add_UndoForwardSum: Command_Numeric_Add_ForwardReverseSum(true, true); break;
				case NECommand.Numeric_Add_UndoReverseSum: Command_Numeric_Add_ForwardReverseSum(false, true); break;
				case NECommand.Numeric_Add_Increment: Command_Numeric_Add_IncrementDecrement(true); break;
				case NECommand.Numeric_Add_Decrement: Command_Numeric_Add_IncrementDecrement(false); break;
				case NECommand.Numeric_Add_AddClipboard: Command_Numeric_Add_AddSubtractClipboard(true); break;
				case NECommand.Numeric_Add_SubtractClipboard: Command_Numeric_Add_AddSubtractClipboard(false); break;
				case NECommand.Numeric_Fraction_Whole: Command_Numeric_Fraction_Whole(); break;
				case NECommand.Numeric_Fraction_Fraction: Command_Numeric_Fraction_Fraction(); break;
				case NECommand.Numeric_Fraction_Simplify: Command_Numeric_Fraction_Simplify(); break;
				case NECommand.Numeric_Absolute: Command_Numeric_Absolute(); break;
				case NECommand.Numeric_Floor: Command_Numeric_Floor(state.Parameters as NumericFloorRoundCeilingDialog.Result); break;
				case NECommand.Numeric_Ceiling: Command_Numeric_Ceiling(state.Parameters as NumericFloorRoundCeilingDialog.Result); break;
				case NECommand.Numeric_Round: Command_Numeric_Round(state.Parameters as NumericFloorRoundCeilingDialog.Result); break;
				case NECommand.Numeric_Limit: Command_Numeric_Limit(state.Parameters as NumericLimitDialog.Result); break;
				case NECommand.Numeric_Cycle: Command_Numeric_Cycle(state.Parameters as NumericCycleDialog.Result); break;
				case NECommand.Numeric_Trim: Command_Numeric_Trim(); break;
				case NECommand.Numeric_Factor: Command_Numeric_Factor(); break;
				case NECommand.Numeric_RandomNumber: Command_Numeric_RandomNumber(state.Parameters as NumericRandomNumberDialog.Result); break;
				case NECommand.Numeric_CombinationsPermutations: Command_Numeric_CombinationsPermutations(state.Parameters as NumericCombinationsPermutationsDialog.Result); break;
				case NECommand.Numeric_MinMaxValues: Command_Numeric_MinMaxValues(state.Parameters as NumericMinMaxValuesDialog.Result); break;
				case NECommand.DateTime_Now: Command_DateTime_Now(); break;
				case NECommand.DateTime_UtcNow: Command_DateTime_UtcNow(); break;
				case NECommand.DateTime_Format: Command_DateTime_Format(state.Parameters as DateTimeFormatDialog.Result); break;
				case NECommand.DateTime_ToUtc: Command_DateTime_ToUtc(); break;
				case NECommand.DateTime_ToLocal: Command_DateTime_ToLocal(); break;
				case NECommand.DateTime_ToTimeZone: Command_DateTime_ToTimeZone(state.Parameters as DateTimeToTimeZoneDialog.Result); break;
				case NECommand.DateTime_AddClipboard: Command_DateTime_AddClipboard(); break;
				case NECommand.DateTime_SubtractClipboard: Command_DateTime_SubtractClipboard(); break;
				case NECommand.Image_GrabColor: Command_Image_GrabColor(state.Parameters as ImageGrabColorDialog.Result); break;
				case NECommand.Image_GrabImage: Command_Image_GrabImage(state.Parameters as ImageGrabImageDialog.Result); break;
				case NECommand.Image_AdjustColor: Command_Image_AdjustColor(state.Parameters as ImageAdjustColorDialog.Result); break;
				case NECommand.Image_AddColor: Command_Image_AddColor(state.Parameters as ImageAddOverlayColorDialog.Result); break;
				case NECommand.Image_OverlayColor: Command_Image_OverlayColor(state.Parameters as ImageAddOverlayColorDialog.Result); break;
				case NECommand.Image_Size: Command_Image_Size(state.Parameters as ImageSizeDialog.Result); break;
				case NECommand.Image_Crop: Command_Image_Crop(state.Parameters as ImageCropDialog.Result); break;
				case NECommand.Image_FlipHorizontal: Command_Image_FlipHorizontal(); break;
				case NECommand.Image_FlipVertical: Command_Image_FlipVertical(); break;
				case NECommand.Image_Rotate: Command_Image_Rotate(state.Parameters as ImageRotateDialog.Result); break;
				case NECommand.Image_GIF_Animate: Command_Image_GIF_Animate(state.Parameters as ImageGIFAnimateDialog.Result); break;
				case NECommand.Image_GIF_Split: Command_Image_GIF_Split(state.Parameters as ImageGIFSplitDialog.Result); break;
				case NECommand.Table_DetectType: Command_Table_DetectType(); break;
				case NECommand.Table_Convert: Command_Table_Convert(state.Parameters as TableConvertDialog.Result); break;
				case NECommand.Table_TextToTable: Command_Table_TextToTable(state.Parameters as TableTextToTableDialog.Result); break;
				case NECommand.Table_LineSelectionsToTable: Command_Table_LineSelectionsToTable(); break;
				case NECommand.Table_RegionSelectionsToTable_Region1: Command_Table_RegionSelectionsToTable_Region(1); break;
				case NECommand.Table_RegionSelectionsToTable_Region2: Command_Table_RegionSelectionsToTable_Region(2); break;
				case NECommand.Table_RegionSelectionsToTable_Region3: Command_Table_RegionSelectionsToTable_Region(3); break;
				case NECommand.Table_RegionSelectionsToTable_Region4: Command_Table_RegionSelectionsToTable_Region(4); break;
				case NECommand.Table_RegionSelectionsToTable_Region5: Command_Table_RegionSelectionsToTable_Region(5); break;
				case NECommand.Table_RegionSelectionsToTable_Region6: Command_Table_RegionSelectionsToTable_Region(6); break;
				case NECommand.Table_RegionSelectionsToTable_Region7: Command_Table_RegionSelectionsToTable_Region(7); break;
				case NECommand.Table_RegionSelectionsToTable_Region8: Command_Table_RegionSelectionsToTable_Region(8); break;
				case NECommand.Table_RegionSelectionsToTable_Region9: Command_Table_RegionSelectionsToTable_Region(9); break;
				case NECommand.Table_EditTable: Command_Table_EditTable(state.Parameters as TableEditTableDialog.Result); break;
				case NECommand.Table_AddHeaders: Command_Table_AddHeaders(); break;
				case NECommand.Table_AddRow: Command_Table_AddRow(); break;
				case NECommand.Table_AddColumn: Command_Table_AddColumn(state.Parameters as TableAddColumnDialog.Result); break;
				case NECommand.Table_Select_RowsByExpression: Command_Table_Select_RowsByExpression(state.Parameters as GetExpressionDialog.Result); break;
				case NECommand.Table_SetJoinSource: Command_Table_SetJoinSource(); break;
				case NECommand.Table_Join: Command_Table_Join(state.Parameters as TableJoinDialog.Result); break;
				case NECommand.Table_Transpose: Command_Table_Transpose(); break;
				case NECommand.Table_Database_GenerateInserts: Command_Table_Database_GenerateInserts(state.Parameters as TableDatabaseGenerateInsertsDialog.Result); break;
				case NECommand.Table_Database_GenerateUpdates: Command_Table_Database_GenerateUpdates(state.Parameters as TableDatabaseGenerateUpdatesDialog.Result); break;
				case NECommand.Table_Database_GenerateDeletes: Command_Table_Database_GenerateDeletes(state.Parameters as TableDatabaseGenerateDeletesDialog.Result); break;
				case NECommand.Position_Goto_Lines: Command_Position_Goto(GotoType.Line, state.ShiftDown, state.Parameters as PositionGotoDialog.Result); break;
				case NECommand.Position_Goto_Columns: Command_Position_Goto(GotoType.Column, state.ShiftDown, state.Parameters as PositionGotoDialog.Result); break;
				case NECommand.Position_Goto_Indexes: Command_Position_Goto(GotoType.Index, state.ShiftDown, state.Parameters as PositionGotoDialog.Result); break;
				case NECommand.Position_Goto_Positions: Command_Position_Goto(GotoType.Position, state.ShiftDown, state.Parameters as PositionGotoDialog.Result); break;
				case NECommand.Position_Copy_Lines: Command_Position_Copy(GotoType.Line, false); break;
				case NECommand.Position_Copy_Columns: Command_Position_Copy(GotoType.Column, !state.ShiftDown); break;
				case NECommand.Position_Copy_Indexes: Command_Position_Copy(GotoType.Index, !state.ShiftDown); break;
				case NECommand.Position_Copy_Positions: Command_Position_Copy(GotoType.Position, false); break;
				case NECommand.Content_Type_SetFromExtension: Command_Content_Type_SetFromExtension(); break;
				case NECommand.Content_Type_None: Command_Content_Type(ParserType.None); break;
				case NECommand.Content_Type_Balanced: Command_Content_Type(ParserType.Balanced); break;
				case NECommand.Content_Type_Columns: Command_Content_Type(ParserType.Columns); break;
				case NECommand.Content_Type_CPlusPlus: Command_Content_Type(ParserType.CPlusPlus); break;
				case NECommand.Content_Type_CSharp: Command_Content_Type(ParserType.CSharp); break;
				case NECommand.Content_Type_CSV: Command_Content_Type(ParserType.CSV); break;
				case NECommand.Content_Type_ExactColumns: Command_Content_Type(ParserType.ExactColumns); break;
				case NECommand.Content_Type_HTML: Command_Content_Type(ParserType.HTML); break;
				case NECommand.Content_Type_JSON: Command_Content_Type(ParserType.JSON); break;
				case NECommand.Content_Type_SQL: Command_Content_Type(ParserType.SQL); break;
				case NECommand.Content_Type_TSV: Command_Content_Type(ParserType.TSV); break;
				case NECommand.Content_Type_XML: Command_Content_Type(ParserType.XML); break;
				case NECommand.Content_HighlightSyntax: Command_Content_HighlightSyntax(state.MultiStatus); break;
				case NECommand.Content_StrictParsing: Command_Content_StrictParsing(state.MultiStatus); break;
				case NECommand.Content_Reformat: Command_Content_Reformat(); break;
				case NECommand.Content_Comment: Command_Content_Comment(); break;
				case NECommand.Content_Uncomment: Command_Content_Uncomment(); break;
				case NECommand.Content_TogglePosition: Command_Content_TogglePosition(state.ShiftDown); break;
				case NECommand.Content_Current: Command_Content_Current(); break;
				case NECommand.Content_Parent: Command_Content_Parent(); break;
				case NECommand.Content_Ancestor: Command_Content_Ancestor(state.Parameters as ContentAttributeDialog.Result); break;
				case NECommand.Content_Attributes: Command_Content_Attributes(state.Parameters as ContentAttributesDialog.Result); break;
				case NECommand.Content_WithAttribute: Command_Content_WithAttribute(state.Parameters as ContentAttributeDialog.Result); break;
				case NECommand.Content_Children_Children: Command_Content_Children_Children(); break;
				case NECommand.Content_Children_SelfAndChildren: Command_Content_Children_SelfAndChildren(); break;
				case NECommand.Content_Children_First: Command_Content_Children_First(); break;
				case NECommand.Content_Children_WithAttribute: Command_Content_Children_WithAttribute(state.Parameters as ContentAttributeDialog.Result); break;
				case NECommand.Content_Descendants_Descendants: Command_Content_Descendants_Descendants(); break;
				case NECommand.Content_Descendants_SelfAndDescendants: Command_Content_Descendants_SelfAndDescendants(); break;
				case NECommand.Content_Descendants_First: Command_Content_Descendants_First(); break;
				case NECommand.Content_Descendants_WithAttribute: Command_Content_Descendants_WithAttribute(state.Parameters as ContentAttributeDialog.Result); break;
				case NECommand.Content_Navigate_Up: Command_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.Up, state.ShiftDown); break;
				case NECommand.Content_Navigate_Down: Command_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.Down, state.ShiftDown); break;
				case NECommand.Content_Navigate_Left: Command_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.Left, state.ShiftDown); break;
				case NECommand.Content_Navigate_Right: Command_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.Right, state.ShiftDown); break;
				case NECommand.Content_Navigate_Home: Command_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.Home, state.ShiftDown); break;
				case NECommand.Content_Navigate_End: Command_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.End, state.ShiftDown); break;
				case NECommand.Content_Navigate_PgUp: Command_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.PgUp, state.ShiftDown); break;
				case NECommand.Content_Navigate_PgDn: Command_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.PgDn, state.ShiftDown); break;
				case NECommand.Content_Navigate_Row: Command_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.Row, true); break;
				case NECommand.Content_Navigate_Column: Command_Content_Navigate(ParserNode.ParserNavigationDirectionEnum.Column, true); break;
				case NECommand.Content_KeepSelections: Command_Content_KeepSelections(state.MultiStatus); break;
				case NECommand.Network_AbsoluteURL: Command_Network_AbsoluteURL(state.Parameters as NetworkAbsoluteURLDialog.Result); break;
				case NECommand.Network_Fetch: Command_Network_Fetch(); break;
				case NECommand.Network_FetchHex: Command_Network_Fetch(Coder.CodePage.Hex); break;
				case NECommand.Network_FetchFile: Command_Network_FetchFile(state.Parameters as NetworkFetchFileDialog.Result); break;
				case NECommand.Network_FetchStream: Command_Network_FetchStream(state.Parameters as NetworkFetchStreamDialog.Result); break;
				case NECommand.Network_FetchPlaylist: Command_Network_FetchPlaylist(state.Parameters as NetworkFetchStreamDialog.Result); break;
				case NECommand.Network_Lookup_IP: Command_Network_Lookup_IP(); break;
				case NECommand.Network_Lookup_HostName: Command_Network_Lookup_HostName(); break;
				case NECommand.Network_AdaptersInfo: Command_Network_AdaptersInfo(); break;
				case NECommand.Network_Ping: Command_Network_Ping(state.Parameters as NetworkPingDialog.Result); break;
				case NECommand.Network_ScanPorts: Command_Network_ScanPorts(state.Parameters as NetworkScanPortsDialog.Result); break;
				case NECommand.Network_WCF_GetConfig: Command_Network_WCF_GetConfig(state.Parameters as NetworkWCFGetConfig.Result); break;
				case NECommand.Network_WCF_Execute: Command_Network_WCF_Execute(); break;
				case NECommand.Network_WCF_InterceptCalls: Command_Network_WCF_InterceptCalls(state.Parameters as NetworkWCFInterceptCallsDialog.Result); break;
				case NECommand.Network_WCF_ResetClients: Command_Network_WCF_ResetClients(); break;
				case NECommand.Database_Connect: Command_Database_Connect(state.Parameters as DatabaseConnectDialog.Result); break;
				case NECommand.Database_ExecuteQuery: Command_Database_ExecuteQuery(); break;
				case NECommand.Database_GetSproc: Command_Database_GetSproc(); break;
				case NECommand.Keys_Set_KeysCaseSensitive: Command_Keys_Set(0, true); break;
				case NECommand.Keys_Set_KeysCaseInsensitive: Command_Keys_Set(0, false); break;
				case NECommand.Keys_Set_Values1: Command_Keys_Set(1); break;
				case NECommand.Keys_Set_Values2: Command_Keys_Set(2); break;
				case NECommand.Keys_Set_Values3: Command_Keys_Set(3); break;
				case NECommand.Keys_Set_Values4: Command_Keys_Set(4); break;
				case NECommand.Keys_Set_Values5: Command_Keys_Set(5); break;
				case NECommand.Keys_Set_Values6: Command_Keys_Set(6); break;
				case NECommand.Keys_Set_Values7: Command_Keys_Set(7); break;
				case NECommand.Keys_Set_Values8: Command_Keys_Set(8); break;
				case NECommand.Keys_Set_Values9: Command_Keys_Set(9); break;
				case NECommand.Keys_Add_Keys: Command_Keys_Add(0); break;
				case NECommand.Keys_Add_Values1: Command_Keys_Add(1); break;
				case NECommand.Keys_Add_Values2: Command_Keys_Add(2); break;
				case NECommand.Keys_Add_Values3: Command_Keys_Add(3); break;
				case NECommand.Keys_Add_Values4: Command_Keys_Add(4); break;
				case NECommand.Keys_Add_Values5: Command_Keys_Add(5); break;
				case NECommand.Keys_Add_Values6: Command_Keys_Add(6); break;
				case NECommand.Keys_Add_Values7: Command_Keys_Add(7); break;
				case NECommand.Keys_Add_Values8: Command_Keys_Add(8); break;
				case NECommand.Keys_Add_Values9: Command_Keys_Add(9); break;
				case NECommand.Keys_Remove_Keys: Command_Keys_Remove(0); break;
				case NECommand.Keys_Remove_Values1: Command_Keys_Remove(1); break;
				case NECommand.Keys_Remove_Values2: Command_Keys_Remove(2); break;
				case NECommand.Keys_Remove_Values3: Command_Keys_Remove(3); break;
				case NECommand.Keys_Remove_Values4: Command_Keys_Remove(4); break;
				case NECommand.Keys_Remove_Values5: Command_Keys_Remove(5); break;
				case NECommand.Keys_Remove_Values6: Command_Keys_Remove(6); break;
				case NECommand.Keys_Remove_Values7: Command_Keys_Remove(7); break;
				case NECommand.Keys_Remove_Values8: Command_Keys_Remove(8); break;
				case NECommand.Keys_Remove_Values9: Command_Keys_Remove(9); break;
				case NECommand.Keys_Replace_Values1: Command_Keys_Replace(1); break;
				case NECommand.Keys_Replace_Values2: Command_Keys_Replace(2); break;
				case NECommand.Keys_Replace_Values3: Command_Keys_Replace(3); break;
				case NECommand.Keys_Replace_Values4: Command_Keys_Replace(4); break;
				case NECommand.Keys_Replace_Values5: Command_Keys_Replace(5); break;
				case NECommand.Keys_Replace_Values6: Command_Keys_Replace(6); break;
				case NECommand.Keys_Replace_Values7: Command_Keys_Replace(7); break;
				case NECommand.Keys_Replace_Values8: Command_Keys_Replace(8); break;
				case NECommand.Keys_Replace_Values9: Command_Keys_Replace(9); break;
				case NECommand.Select_All: Command_Select_All(); break;
				case NECommand.Select_Nothing: Command_Select_Nothing(); break;
				case NECommand.Select_Limit: Command_Select_Limit(state.Parameters as SelectLimitDialog.Result); break;
				case NECommand.Select_Lines: Command_Select_Lines(); break;
				case NECommand.Select_WholeLines: Command_Select_WholeLines(); break;
				case NECommand.Select_Rectangle: Command_Select_Rectangle(); break;
				case NECommand.Select_Invert: Command_Select_Invert(); break;
				case NECommand.Select_Join: Command_Select_Join(); break;
				case NECommand.Select_Empty: Command_Select_Empty(true); break;
				case NECommand.Select_NonEmpty: Command_Select_Empty(false); break;
				case NECommand.Select_ToggleOpenClose: Command_Select_ToggleOpenClose(state.ShiftDown); break;
				case NECommand.Select_RepeatsCaseSensitive_Unique: Command_Select_Repeats_Unique(true); break;
				case NECommand.Select_RepeatsCaseSensitive_Duplicates: Command_Select_Repeats_Duplicates(true); break;
				case NECommand.Select_RepeatsCaseSensitive_MatchPrevious: Command_Select_Repeats_MatchPrevious(true); break;
				case NECommand.Select_RepeatsCaseSensitive_NonMatchPrevious: Command_Select_Repeats_NonMatchPrevious(true); break;
				case NECommand.Select_RepeatsCaseSensitive_RepeatedLines: Command_Select_Repeats_RepeatedLines(true); break;
				case NECommand.Select_RepeatsCaseSensitive_ByCount: Command_Select_Repeats_ByCount(state.Parameters as SelectByCountDialog.Result, true); break;
				case NECommand.Select_RepeatsCaseSensitive_Tabs_Match: Command_Select_Repeats_Tabs_MatchMismatch(state.PreHandleData, true); break;
				case NECommand.Select_RepeatsCaseSensitive_Tabs_Mismatch: Command_Select_Repeats_Tabs_MatchMismatch(state.PreHandleData, false); break;
				case NECommand.Select_RepeatsCaseSensitive_Tabs_Common: Command_Select_Repeats_Tabs_CommonNonCommon(state.PreHandleData, true); break;
				case NECommand.Select_RepeatsCaseSensitive_Tabs_NonCommon: Command_Select_Repeats_Tabs_CommonNonCommon(state.PreHandleData, false); break;
				case NECommand.Select_RepeatsCaseInsensitive_Unique: Command_Select_Repeats_Unique(false); break;
				case NECommand.Select_RepeatsCaseInsensitive_Duplicates: Command_Select_Repeats_Duplicates(false); break;
				case NECommand.Select_RepeatsCaseInsensitive_MatchPrevious: Command_Select_Repeats_MatchPrevious(false); break;
				case NECommand.Select_RepeatsCaseInsensitive_NonMatchPrevious: Command_Select_Repeats_NonMatchPrevious(false); break;
				case NECommand.Select_RepeatsCaseInsensitive_RepeatedLines: Command_Select_Repeats_RepeatedLines(false); break;
				case NECommand.Select_RepeatsCaseInsensitive_ByCount: Command_Select_Repeats_ByCount(state.Parameters as SelectByCountDialog.Result, false); break;
				case NECommand.Select_RepeatsCaseInsensitive_Tabs_Match: Command_Select_Repeats_Tabs_MatchMismatch(state.PreHandleData, true); break;
				case NECommand.Select_RepeatsCaseInsensitive_Tabs_Mismatch: Command_Select_Repeats_Tabs_MatchMismatch(state.PreHandleData, false); break;
				case NECommand.Select_RepeatsCaseInsensitive_Tabs_Common: Command_Select_Repeats_Tabs_CommonNonCommon(state.PreHandleData, true); break;
				case NECommand.Select_RepeatsCaseInsensitive_Tabs_NonCommon: Command_Select_Repeats_Tabs_CommonNonCommon(state.PreHandleData, false); break;
				case NECommand.Select_Split: Command_Select_Split(state.Parameters as SelectSplitDialog.Result); break;
				case NECommand.Select_Selection_First: Command_Select_Selection_First(); break;
				case NECommand.Select_Selection_CenterVertically: Command_Select_Selection_CenterVertically(); break;
				case NECommand.Select_Selection_Center: Command_Select_Selection_Center(); break;
				case NECommand.Select_Selection_ToggleAnchor: Command_Select_Selection_ToggleAnchor(state.PreHandleData); break;
				case NECommand.Select_Selection_Next: Command_Select_Selection_NextPrevious(true); break;
				case NECommand.Select_Selection_Previous: Command_Select_Selection_NextPrevious(false); break;
				case NECommand.Select_Selection_Single: Command_Select_Selection_Single(); break;
				case NECommand.Select_Selection_Remove: Command_Select_Selection_Remove(); break;
				case NECommand.Select_Selection_RemoveBeforeCurrent: Command_Select_Selection_RemoveBeforeCurrent(); break;
				case NECommand.Select_Selection_RemoveAfterCurrent: Command_Select_Selection_RemoveAfterCurrent(); break;
				//TODO
				//case NECommand.Macro_RepeatLastAction: Command_Macro_RepeatLastAction(); break;
				//case NECommand.Macro_TimeNextAction: timeNext = !timeNext; break;
				case NECommand.Window_TabIndex: Command_Window_TabIndex(false); break;
				case NECommand.Window_ActiveTabIndex: Command_Window_TabIndex(true); break;
				case NECommand.Window_ViewValues: Command_Window_ViewValues(state.MultiStatus); break;
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

		void PreCommand_Key(Key key, ref object previousData)
		{
			switch (key)
			{
				case Key.Back:
				case Key.Delete:
				case Key.Left:
				case Key.Right:
					previousData = (previousData as bool? ?? false) || (Selections.Any(range => range.HasSelection));
					break;
			}
		}

		bool Command_Internal_Key(Key key, bool shiftDown, bool controlDown, bool altDown, object previousData)
		{
			var ret = true;
			switch (key)
			{
				case Key.Back:
				case Key.Delete:
					{
						if ((bool)previousData)
						{
							ReplaceSelections("");
							break;
						}

						Replace(Selections.AsParallel().AsOrdered().Select(range =>
						{
							var position = range.Start;
							var anchor = range.Anchor;

							if (controlDown)
							{
								if (key == Key.Back)
									position = GetPrevWord(position);
								else
									position = GetNextWord(position);
							}
							else if ((shiftDown) && (key == Key.Delete))
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
							if ((!shiftDown) && ((bool)previousData))
								return new Range(range.Start);
							else if ((index == 0) && (line != 0))
								return MoveCursor(range, -1, int.MaxValue, shiftDown, indexRel: false);
							else
								return MoveCursor(range, 0, -1, shiftDown);
						}).ToList();
					}
					break;
				case Key.Right:
					{
						Selections = Selections.AsParallel().AsOrdered().Select(range =>
						{
							var line = TextView.GetPositionLine(range.Cursor);
							var index = TextView.GetPositionIndex(range.Cursor, line);
							if ((!shiftDown) && ((bool)previousData))
								return new Range(range.End);
							else if ((index == TextView.GetLineLength(line)) && (line != TextView.NumLines - 1))
								return MoveCursor(range, 1, 0, shiftDown, indexRel: false);
							else
								return MoveCursor(range, 0, 1, shiftDown);
						}).ToList();
					}
					break;
				case Key.Up:
				case Key.Down:
					{
						var mult = key == Key.Up ? -1 : 1;
						if (!controlDown)
							Selections = Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, mult, 0, shiftDown)).ToList();
						else if (!shiftDown)
							YScrollValue += mult;
						else if (key == Key.Down)
							BlockSelDown();
						else
							BlockSelUp();
					}
					break;
				case Key.Home:
					if (controlDown)
					{
						var sels = Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, 0, shiftDown)).ToList(); // Have to use MoveCursor for selection
						if ((!sels.Any()) && (!shiftDown))
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
							sels.Add(MoveCursor(selection, 0, first, shiftDown, indexRel: false));
						}
						if (!changed)
						{
							sels = sels.AsParallel().AsOrdered().Select(range => MoveCursor(range, 0, 0, shiftDown, indexRel: false)).ToList();
							XScrollValue = 0;
						}
						Selections = sels;
					}
					break;
				case Key.End:
					if (controlDown)
					{
						var sels = Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, Text.Length, shiftDown)).ToList(); // Have to use MoveCursor for selection
						if ((!sels.Any()) && (!shiftDown))
							sels.Add(new Range(Text.Length));
						Selections = sels;
					}
					else
						Selections = Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, 0, int.MaxValue, shiftDown, indexRel: false)).ToList();
					break;
				case Key.PageUp:
					if (controlDown)
						YScrollValue -= YScrollViewportFloor / 2;
					else
					{
						var savedYScrollViewportFloor = YScrollViewportFloor;
						Selections = Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, 1 - savedYScrollViewportFloor, 0, shiftDown)).ToList();
					}
					break;
				case Key.PageDown:
					if (controlDown)
						YScrollValue += YScrollViewportFloor / 2;
					else
					{
						var savedYScrollViewportFloor = YScrollViewportFloor;
						Selections = Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, savedYScrollViewportFloor - 1, 0, shiftDown)).ToList();
					}
					break;
				case Key.Tab:
					{
						if (Selections.AsParallel().All(range => (!range.HasSelection) || (TextView.GetPositionLine(range.Start) == TextView.GetPositionLine(Math.Max(range.Start, range.End - 1)))))
						{
							if (!shiftDown)
								Command_Internal_Text("\t");
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
						if (shiftDown)
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
					Command_Internal_Text(TextView.DefaultEnding);
					break;
				default: ret = false; break;
			}

			return ret;
		}

		void Command_Internal_Text(string text) => ReplaceSelections(text, false, tryJoinUndo: true);

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

			//for (var ctr = 0; ctr < 10; ++ctr)
			//{
			//	var name = ctr == 0 ? "k" : $"v{ctr}";
			//	var desc = ctr == 0 ? "Keys" : $"Values {ctr}";
			//	var values = TabsParent.GetKeysAndValues(this, ctr, false);
			//	if (values == null)
			//		continue;
			//	results.Add(NEVariable.List(name, desc, () => values));
			//	results.Add(NEVariable.Constant($"{name}n", $"{desc} count", () => values.Count));
			//	results.Add(NEVariable.List($"{name}l", $"{desc} length", () => values.Select(str => str.Length)));
			//	results.Add(NEVariable.Constant($"{name}lmin", $"{desc} min length", () => values.Select(str => str.Length).DefaultIfEmpty(0).Min()));
			//	results.Add(NEVariable.Constant($"{name}lmax", $"{desc} max length", () => values.Select(str => str.Length).DefaultIfEmpty(0).Max()));
			//}

			//if (Coder.IsImage(CodePage))
			//{
			//	results.Add(NEVariable.Constant("width", "Image width", () => GetBitmap().Width));
			//	results.Add(NEVariable.Constant("height", "Image height", () => GetBitmap().Height));
			//}

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

		public TabsWindow TabsParent = null; // TODO

		static ThreadSafeRandom random = new ThreadSafeRandom();

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

		public void SetClipboardStrings(IEnumerable<string> strs) => TabsParent.AddClipboardStrings(strs);

		public void SetClipboardFiles(IEnumerable<string> fileNames, bool isCut = false) => TabsParent.AddClipboardStrings(fileNames, isCut);

		public void ReplaceOneWithMany(List<string> strs, bool? addNewLines)
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

		public bool CheckCanEncode(IEnumerable<byte[]> datas, Coder.CodePage codePage) => (datas.AsParallel().All(data => Coder.CanEncode(data, codePage))) || (ConfirmContinueWhenCannotEncode());

		public bool CheckCanEncode(IEnumerable<string> strs, Coder.CodePage codePage) => (strs.AsParallel().All(str => Coder.CanEncode(str, codePage))) || (ConfirmContinueWhenCannotEncode());

		bool ConfirmContinueWhenCannotEncode()
		{
			if (!savedAnswers[nameof(ConfirmContinueWhenCannotEncode)].HasFlag(MessageOptions.All))
				savedAnswers[nameof(ConfirmContinueWhenCannotEncode)] = new Message(TabsParent)
				{
					Title = "Confirm",
					Text = "The specified encoding cannot fully represent the data. Continue anyway?",
					Options = MessageOptions.YesNoAll,
					DefaultAccept = MessageOptions.Yes,
					DefaultCancel = MessageOptions.No,
				}.Show();
			return savedAnswers[nameof(ConfirmContinueWhenCannotEncode)].HasFlag(MessageOptions.Yes);
		}

		public DbConnection dbConnection { get; set; }

		public void OpenTable(Table table, string name = null)
		{
			//TODO
			//var contentType = ContentType.IsTableType() ? ContentType : ParserType.Columns;
			//var textEditor = new TextEditor(bytes: Coder.StringToBytes(table.ToString("\r\n", contentType), Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, modified: false);
			//TabsParent.AddTextEditor(textEditor);
			//textEditor.ContentType = contentType;
			//textEditor.DisplayName = name;
		}

		public List<string> RelativeSelectedFiles()
		{
			var fileName = FileName;
			return Selections.AsParallel().AsOrdered().Select(range => fileName.RelativeChild(Text.GetString(range))).ToList();
		}

		public void Save(string fileName, bool copyOnly = false)
		{
			//if ((Coder.IsStr(CodePage)) && ((DataQwer.MaxPosition >> 20) < 50) && (!VerifyCanEncode()))
			//	return;

			//var triedReadOnly = false;
			//while (true)
			//{
			//	try
			//	{
			//		if ((!copyOnly) && (watcher != null))
			//			watcher.EnableRaisingEvents = false;
			//		File.WriteAllBytes(fileName, FileSaver.Encrypt(FileSaver.Compress(DataQwer.GetBytes(CodePage), Compressed), AESKey));
			//		if ((!copyOnly) && (watcher != null))
			//			watcher.EnableRaisingEvents = true;
			//		break;
			//	}
			//	catch (UnauthorizedAccessException)
			//	{
			//		if ((triedReadOnly) || (!new FileInfo(fileName).IsReadOnly))
			//			throw;

			//		if (!savedAnswers[nameof(Save)].HasFlag(MessageOptions.All))
			//			savedAnswers[nameof(Save)] = new Message(TabsParent)
			//			{
			//				Title = "Confirm",
			//				Text = "Save failed. Remove read-only flag?",
			//				Options = MessageOptions.YesNoAll,
			//				DefaultAccept = MessageOptions.Yes,
			//				DefaultCancel = MessageOptions.No,
			//			}.Show();
			//		if (!savedAnswers[nameof(Save)].HasFlag(MessageOptions.Yes))
			//			throw;
			//		new FileInfo(fileName).IsReadOnly = false;
			//		triedReadOnly = true;
			//	}
			//}

			//if (!copyOnly)
			//{
			//	fileLastWrite = new FileInfo(fileName).LastWriteTime;
			//	SetModifiedFlag(false);
			//	SetFileName(fileName);
			//}
		}

		public void SetClipboardFile(string fileName, bool isCut = false) => SetClipboardFiles(new List<string> { fileName }, isCut);

		public void SetClipboardString(string text) => SetClipboardStrings(new List<string> { text });

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

			if (!savedAnswers[nameof(CanClose)].HasFlag(MessageOptions.All))
				savedAnswers[nameof(CanClose)] = new Message(TabsParent)
				{
					Title = "Confirm",
					Text = "Do you want to save changes?",
					Options = MessageOptions.YesNoAllCancel,
					DefaultCancel = MessageOptions.Cancel,
				}.Show(false);

			if (savedAnswers[nameof(CanClose)].HasFlag(MessageOptions.No))
				return true;
			if (savedAnswers[nameof(CanClose)].HasFlag(MessageOptions.Yes))
			{
				Command_File_Save_Save();
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

			FileSaver.HandleDecrypt(TabsParent, ref bytes, out var aesKey);
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

		public string OnlyEnding { get; internal set; }

		void OnStatusBarRender()
		{
			var sb = new List<string>();

			ViewValuesData = null;
			ViewValuesHasSel = false;

			if ((CurrentSelection < 0) || (CurrentSelection >= Selections.Count))
			{
				sb.Add("Selection 0/0");
				sb.Add("Col");
				sb.Add("In");
				sb.Add("Pos");
			}
			else
			{
				var range = Selections[CurrentSelection];
				var lineMin = TextView.GetPositionLine(range.Start);
				var lineMax = TextView.GetPositionLine(range.End);
				var indexMin = TextView.GetPositionIndex(range.Start, lineMin);
				var indexMax = TextView.GetPositionIndex(range.End, lineMax);
				var columnMin = GetColumnFromIndex(lineMin, indexMin);
				var columnMax = GetColumnFromIndex(lineMax, indexMax);
				var posMin = range.Start;
				var posMax = range.End;

				try
				{
					ViewValuesData = Coder.StringToBytes(Text.GetString(range.Start, Math.Min(range.HasSelection ? range.Length : 100, Text.Length - range.Start)), CodePage);
					ViewValuesHasSel = range.HasSelection;
				}
				catch { }

				sb.Add($"Selection {CurrentSelection + 1:n0}/{Selections.Count:n0}");
				sb.Add($"Col {lineMin + 1:n0}:{columnMin + 1:n0}{((lineMin == lineMax) && (columnMin == columnMax) ? "" : $"-{(lineMin == lineMax ? "" : $"{lineMax + 1:n0}:")}{columnMax + 1:n0}")}");
				sb.Add($"In {lineMin + 1:n0}:{indexMin + 1:n0}{((lineMin == lineMax) && (indexMin == indexMax) ? "" : $"-{(lineMin == lineMax ? "" : $"{lineMax + 1:n0}:")}{indexMax + 1:n0}")}");
				sb.Add($"Pos {posMin:n0}{(posMin == posMax ? "" : $"-{posMax:n0} ({posMax - posMin:n0})")}");
			}

			sb.Add($"Regions {string.Join(" / ", Enumerable.Range(1, 9).ToDictionary(index => index, index => GetRegions(index)).OrderBy(pair => pair.Key).Select(pair => $"{pair.Value:n0}"))}");
			sb.Add($"Database {DBName}");

			var tf = SystemFonts.MessageFontFamily.GetTypefaces().Where(x => (x.Weight == FontWeights.Normal) && (x.Style == FontStyles.Normal)).First();
			//TODO dc.DrawText(new FormattedText(string.Join(" │ ", sb), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, tf, SystemFonts.MessageFontSize, Brushes.White, 1), new Point(2, 2));
		}

		public bool HasSelections => Selections.Any();

		public int NumSelections => Selections.Count;

		public int Length => Text.Length;

		public string GetString(int start, int length) => Text.GetString(start, length);
	}
}
