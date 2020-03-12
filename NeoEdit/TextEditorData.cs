using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using NeoEdit.Program.Dialogs;
using NeoEdit.Program.Expressions;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program
{
	public partial class TextEditorData
	{
		const int tabStop = 4;

		public TextEditorData()
		{
			Text = new NEText(File.ReadAllText(@"..\..\a.txt"));
			Selections = new List<Range>();
			for (var region = 1; region <= 9; ++region)
				SetRegions(region, new List<Range>());
			undoRedo = newUndoRedo = new UndoRedo2();
			Commit();
		}

		UndoRedo2 undoRedo, newUndoRedo;
		CacheValue2 modifiedChecksum = new CacheValue2();

		public void ReplaceSelections(string str, bool highlight = true, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false) => ReplaceSelections(Enumerable.Repeat(str, Selections.Count).ToList(), highlight, replaceType, tryJoinUndo);

		public void ReplaceSelections(List<string> strs, bool highlight = true, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false)
		{
			Replace(Selections.ToList(), strs, replaceType, tryJoinUndo);

			if (highlight)
				SetSelections(Selections.AsParallel().AsOrdered().Select((range, index) => new Range(range.End, range.End - (strs == null ? 0 : strs[index].Length))).ToList());
			else
				SetSelections(Selections.AsParallel().AsOrdered().Select(range => new Range(range.End)).ToList());
		}

		public void Replace(List<Range> ranges, List<string> strs, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false)
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

			var textCanvasUndoRedo = new UndoRedo2.UndoRedoStep(undoRanges, undoText, tryJoinUndo);
			switch (replaceType)
			{
				case ReplaceType.Undo: UndoRedo2.AddUndone(ref newUndoRedo, textCanvasUndoRedo); break;
				case ReplaceType.Redo: UndoRedo2.AddRedone(ref newUndoRedo, textCanvasUndoRedo); break;
				case ReplaceType.Normal: UndoRedo2.AddUndo(ref newUndoRedo, textCanvasUndoRedo, IsModified); break;
			}

			Text = Text.Replace(ranges, strs);
			SetModifiedFlag();

			var translateMap = GetTranslateMap(ranges, strs, new List<List<Range>> { Selections }.Concat(Enumerable.Range(1, 9).Select(region => GetRegions(region))).ToList());
			SetSelections(Translate(Selections, translateMap));
			for (var region = 1; region <= 9; ++region)
				SetRegions(region, Translate(GetRegions(region), translateMap));
		}

		public int NumLines => TextView.NumLines;

		public void SetSelections(List<Range> selections) => Selections = selections;

		#region Translate
		static int[] GetTranslateNums(List<List<Range>> rangeLists)
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

		static Tuple<int[], int[]> GetTranslateMap(List<Range> replaceRanges, List<string> strs, List<List<Range>> rangeLists)
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

		static List<Range> Translate(List<Range> ranges, Tuple<int[], int[]> translateMap)
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

		static List<Range> DeOverlap(List<Range> items)
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

		static DeOverlapStep GetDeOverlapStep(List<Range> items)
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

		static List<Range> DoDeOverlap(List<Range> items)
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
				case NECommand.Internal_Key: PreCommand_Key((Key)state.Parameters, ref state.PreHandleData); break;
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
				case NECommand.Select_Limit: state.Parameters = Command_Select_Limit_Dialog(TabsParent); break;
				case NECommand.Select_RepeatsCaseSensitive_ByCount: state.Parameters = Command_Select_Repeats_ByCount_Dialog(TabsParent); break;
				case NECommand.Select_RepeatsCaseInsensitive_ByCount: state.Parameters = Command_Select_Repeats_ByCount_Dialog(TabsParent); break;
				case NECommand.Select_Split: state.Parameters = Command_Select_Split_Dialog(TabsParent); break;
			}
		}

		public void ExecuteCommand(CommandState state)
		{
			switch (state.Command)
			{
				case NECommand.Internal_Key: state.Result = Command_Internal_Key((Key)state.Parameters, state.ShiftDown, state.ControlDown, state.AltDown, state.PreHandleData); break;
				case NECommand.Internal_Text: Command_Internal_Text(state.Parameters as string); break;
				case NECommand.Edit_Undo: Command_Edit_Undo(); break;
				case NECommand.Edit_Redo: Command_Edit_Redo(); break;
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
			SetSelections(Selections.Concat(sels).ToList());
		}

		void BlockSelUp()
		{
			var found = new HashSet<string>();
			foreach (var range in Selections)
				found.Add(range.ToString());

			var sels = new List<Range>();
			foreach (var range in Selections.ToList())
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

			SetSelections(sels);
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
						//	SetSelections(new List<Range> { new Range(pos) });
						//}
					}
					break;
				case Key.Left:
					{
						SetSelections(Selections.AsParallel().AsOrdered().Select(range =>
						{
							var line = TextView.GetPositionLine(range.Cursor);
							var index = TextView.GetPositionIndex(range.Cursor, line);
							if ((!shiftDown) && ((bool)previousData))
								return new Range(range.Start);
							else if ((index == 0) && (line != 0))
								return MoveCursor(range, -1, int.MaxValue, shiftDown, indexRel: false);
							else
								return MoveCursor(range, 0, -1, shiftDown);
						}).ToList());
					}
					break;
				case Key.Right:
					{
						SetSelections(Selections.AsParallel().AsOrdered().Select(range =>
						{
							var line = TextView.GetPositionLine(range.Cursor);
							var index = TextView.GetPositionIndex(range.Cursor, line);
							if ((!shiftDown) && ((bool)previousData))
								return new Range(range.End);
							else if ((index == TextView.GetLineLength(line)) && (line != TextView.NumLines - 1))
								return MoveCursor(range, 1, 0, shiftDown, indexRel: false);
							else
								return MoveCursor(range, 0, 1, shiftDown);
						}).ToList());
					}
					break;
				case Key.Up:
				case Key.Down:
					{
						var mult = key == Key.Up ? -1 : 1;
						if (!controlDown)
							SetSelections(Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, mult, 0, shiftDown)).ToList());
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
						SetSelections(sels);
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
						SetSelections(sels);
					}
					break;
				case Key.End:
					if (controlDown)
					{
						var sels = Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, Text.Length, shiftDown)).ToList(); // Have to use MoveCursor for selection
						if ((!sels.Any()) && (!shiftDown))
							sels.Add(new Range(Text.Length));
						SetSelections(sels);
					}
					else
						SetSelections(Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, 0, int.MaxValue, shiftDown, indexRel: false)).ToList());
					break;
				case Key.PageUp:
					if (controlDown)
						YScrollValue -= YScrollViewportFloor / 2;
					else
					{
						var savedYScrollViewportFloor = YScrollViewportFloor;
						SetSelections(Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, 1 - savedYScrollViewportFloor, 0, shiftDown)).ToList());
					}
					break;
				case Key.PageDown:
					if (controlDown)
						YScrollValue += YScrollViewportFloor / 2;
					else
					{
						var savedYScrollViewportFloor = YScrollViewportFloor;
						SetSelections(Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, savedYScrollViewportFloor - 1, 0, shiftDown)).ToList());
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
	}
}
