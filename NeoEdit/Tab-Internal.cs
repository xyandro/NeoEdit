using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Input;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program
{
	partial class Tab
	{
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

		object Configure_Internal_Key()
		{
			switch (state.Key)
			{
				case Key.Back:
				case Key.Delete:
				case Key.Left:
				case Key.Right:
					return state.ActiveTabs.Any(tab => tab.Selections.Any(range => range.HasSelection));
				default: return null;
			}
		}

		void Execute_Internal_Key()
		{
			if (state.Handled)
				return;

			state.Handled = true;

			switch (state.Key)
			{
				case Key.Back:
				case Key.Delete:
					{
						if ((bool)state.Configuration)
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
								if (state.Key == Key.Back)
									position = GetPrevWord(position);
								else
									position = GetNextWord(position);
							}
							else if ((state.ShiftDown) && (state.Key == Key.Delete))
							{
								var line = TextView.GetPositionLine(position);
								position = TextView.GetPosition(line, 0);
								anchor = position + TextView.GetLineLength(line) + TextView.GetEndingLength(line);
							}
							else
							{
								var line = TextView.GetPositionLine(position);
								var index = TextView.GetPositionIndex(position, line);

								if (state.Key == Key.Back)
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
						}).Where(range => range != null).ToList());
					}
					break;
				case Key.Escape:
					//DragFiles = null;
					if (Settings.EscapeClearsSelections)
					{
						Execute_Select_Selection_Single();
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
							if ((!state.ShiftDown) && ((bool)state.Configuration))
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
							if ((!state.ShiftDown) && ((bool)state.Configuration))
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
						var mult = state.Key == Key.Up ? -1 : 1;
						if (!state.ControlDown)
							Selections = Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, mult, 0, state.ShiftDown)).ToList();
						else if (!state.ShiftDown)
							YScrollValue += mult;
						else if (state.Key == Key.Down)
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
								ReplaceSelections("\t", false, tryJoinUndo: true);
							else
							{
								var tabs = Selections.AsParallel().AsOrdered().Where(range => (range.Start != 0) && (Text.GetString(range.Start - 1, 1) == "\t")).Select(range => Range.FromIndex(range.Start - 1, 1)).ToList();
								Replace(tabs, Enumerable.Repeat("", tabs.Count).ToList());
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
					ReplaceSelections(TextView.DefaultEnding, false, tryJoinUndo: true);
					break;
				default: state.Handled = false; break;
			}
		}

		void Execute_Internal_Text() => ReplaceSelections(state.Text, false, tryJoinUndo: true);

		void Execute_Internal_SetViewValue()
		{
			(var value, var size) = ((byte[], int?))state.Configuration;
			var sels = Selections.Select(range => Range.FromIndex(range.Start, size ?? range.Length)).ToList();
			var values = Enumerable.Repeat(Coder.BytesToString(value, CodePage), sels.Count).ToList();
			Replace(sels, values);
		}
	}
}
