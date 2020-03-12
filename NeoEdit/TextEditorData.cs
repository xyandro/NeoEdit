using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
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
				case NECommand.Select_Lines: Command_Select_Lines(); break;
				case NECommand.Select_WholeLines: Command_Select_WholeLines(); break;
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
	}
}
