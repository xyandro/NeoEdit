using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Transform;
using NeoEdit.TaskRunning;

namespace NeoEdit.Editor
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

		Range MoveCursor(Range range, int position, bool selecting)
		{
			position = Math.Max(0, Math.Min(position, Text.Length));
			if (selecting)
				if (range.Cursor == position)
					return range;
				else
					return new Range(position, range.Anchor);

			if ((range.Cursor == position) && (range.Anchor == position))
				return range;
			return new Range(position);
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

		Configuration_Internal_Key Configure_Internal_Key()
		{
			switch (state.Key)
			{
				case Key.Back:
				case Key.Delete:
				case Key.Left:
				case Key.Right:
					return new Configuration_Internal_Key { HasSelections = Tabs.ActiveTabs.Any(tab => tab.Selections.Any(range => range.HasSelection)) };
				default: return null;
			}
		}

		void Execute_Internal_Key()
		{
			switch (state.Key)
			{
				case Key.Back:
				case Key.Delete:
					{
						if ((state.Configuration as Configuration_Internal_Key).HasSelections)
						{
							ReplaceSelections("");
							break;
						}

						Replace(Selections.AsTaskRunner().Select(range =>
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
					if (ViewBinarySearches != null)
					{
						ViewBinarySearches = null;
						break;
					}
					//DragFiles = null;
					if (Settings.EscapeClearsSelections)
					{
						Execute_Select_Selection_Single();
						if (!Selections.Any())
							Selections = new List<Range> { new Range() };
					}
					break;
				case Key.Left:
					{
						Selections = Selections.AsTaskRunner().Select(range =>
						{
							if ((!state.ShiftDown) && ((state.Configuration as Configuration_Internal_Key).HasSelections))
								return new Range(range.Start);

							var line = TextView.GetPositionLine(range.Cursor);
							var index = TextView.GetPositionIndex(range.Cursor, line);
							if ((index == 0) && (line != 0))
								return MoveCursor(range, range.Cursor - Math.Max(1, TextView.GetEndingLength(line - 1)), state.ShiftDown);
							else
								return MoveCursor(range, range.Cursor - 1, state.ShiftDown);
						}).ToList();
					}
					break;
				case Key.Right:
					{
						Selections = Selections.AsTaskRunner().Select(range =>
						{
							if ((!state.ShiftDown) && ((state.Configuration as Configuration_Internal_Key).HasSelections))
								return new Range(range.End);

							var line = TextView.GetPositionLine(range.Cursor);
							var index = TextView.GetPositionIndex(range.Cursor, line);
							if ((index == TextView.GetLineLength(line)) && (line != TextView.NumLines - 1))
								return MoveCursor(range, range.Cursor + Math.Max(1, TextView.GetEndingLength(line)), state.ShiftDown);
							else
								return MoveCursor(range, range.Cursor + 1, state.ShiftDown);
						}).ToList();
					}
					break;
				case Key.Up:
				case Key.Down:
					{
						var mult = state.Key == Key.Up ? -1 : 1;
						if (!state.ControlDown)
							Selections = Selections.AsTaskRunner().Select(range => MoveCursor(range, mult, 0, state.ShiftDown)).ToList();
						else if (!state.ShiftDown)
							StartRow += mult;
						else if (state.Key == Key.Down)
							BlockSelDown();
						else
							BlockSelUp();
					}
					break;
				case Key.Home:
					if (state.ControlDown)
					{
						var sels = Selections.AsTaskRunner().Select(range => MoveCursor(range, 0, state.ShiftDown)).ToList(); // Have to use MoveCursor for selection
						if ((!sels.Any()) && (!state.ShiftDown))
							sels = new List<Range> { new Range() };
						Selections = sels;
					}
					else
					{
						var startTextSels = new List<Range>();
						var startLineSels = new List<Range>();
						bool moveToStartText = false;
						foreach (var selection in Selections)
						{
							var line = TextView.GetPositionLine(selection.Cursor);
							var index = TextView.GetPositionIndex(selection.Cursor, line);

							int startText;
							var end = TextView.GetLineLength(line);
							for (startText = 0; startText < end; ++startText)
							{
								if (!char.IsWhiteSpace(Text[TextView.GetPosition(line, startText)]))
									break;
							}
							if (startText == end)
								startText = 0;

							if (startText != index)
								moveToStartText = true;
							startTextSels.Add(MoveCursor(selection, TextView.GetPosition(line, startText), state.ShiftDown));
							startLineSels.Add(MoveCursor(selection, TextView.GetPosition(line, 0), state.ShiftDown));
						}
						if (moveToStartText)
							Selections = startTextSels;
						else
							Selections = startLineSels;
					}
					break;
				case Key.End:
					if (state.ControlDown)
					{
						var sels = Selections.AsTaskRunner().Select(range => MoveCursor(range, Text.Length, state.ShiftDown)).ToList(); // Have to use MoveCursor for selection
						if ((!sels.Any()) && (!state.ShiftDown))
							sels = new List<Range> { new Range(Text.Length) };
						Selections = sels;
					}
					else
						Selections = Selections.AsTaskRunner().Select(range => MoveCursor(range, 0, int.MaxValue, state.ShiftDown, indexRel: false)).ToList();
					break;
				case Key.PageUp:
					if (state.ControlDown)
						StartRow -= Tabs.TabRows / 2;
					else
						Selections = Selections.AsTaskRunner().Select(range => MoveCursor(range, 1 - Tabs.TabRows, 0, state.ShiftDown)).ToList();
					break;
				case Key.PageDown:
					if (state.ControlDown)
						StartRow += Tabs.TabRows / 2;
					else
						Selections = Selections.AsTaskRunner().Select(range => MoveCursor(range, Tabs.TabRows - 1, 0, state.ShiftDown)).ToList();
					break;
				case Key.Tab:
					{
						if (Selections.AsTaskRunner().All(range => (!range.HasSelection) || (TextView.GetPositionLine(range.Start) == TextView.GetPositionLine(Math.Max(range.Start, range.End - 1)))))
						{
							if (!state.ShiftDown)
								ReplaceSelections("\t", false, tryJoinUndo: true);
							else
							{
								var tabs = Selections.AsTaskRunner().Where(range => (range.Start != 0) && (Text.GetString(range.Start - 1, 1) == "\t")).Select(range => Range.FromIndex(range.Start - 1, 1)).ToList();
								Replace(tabs, Enumerable.Repeat("", tabs.Count).ToList());
							}
							break;
						}

						var selLines = Selections.AsTaskRunner().Where(a => a.HasSelection).Select(range => new { start = TextView.GetPositionLine(range.Start), end = TextView.GetPositionLine(range.End - 1) }).ToList();
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
				default: throw new OperationCanceledException();
			}
		}

		void Execute_Internal_Text() => ReplaceSelections(state.Text, false, tryJoinUndo: true);

		void Execute_Internal_SetBinaryValue()
		{
			var configuration = state.Configuration as Configuration_Internal_SetBinaryValue;
			var newStr = Coder.BytesToString(configuration.Value, CodePage);
			var replaceRange = Selections[CurrentSelection];
			var newSels = new List<Range>();
			var offset = 0;
			foreach (var range1 in Selections)
			{
				var range = range1;
				if (range == replaceRange)
				{
					replaceRange = Range.FromIndex(replaceRange.Start, configuration.OldSize ?? replaceRange.Length);
					offset += newStr.Length - replaceRange.Length;
					range = Range.FromIndex(range.Start, range.Length + offset);
				}
				else
					range = range.Move(offset);
				newSels.Add(range);
			}
			Replace(new List<Range> { replaceRange }, new List<string> { newStr });
			Selections = newSels;
		}

		public void Execute_Internal_Mouse(int line, int column, int clickCount, bool? selecting)
		{
			var sels = Selections.ToList();
			line = Math.Max(0, Math.Min(line, DiffView.NumLines - 1));
			column = Math.Max(0, Math.Min(column, DiffView.GetLineColumnsLength(Text, line)));
			var index = DiffView.GetIndexFromColumn(Text, line, column, true);
			var position = DiffView.GetPosition(line, index);
			var mouseRange = (CurrentSelection >= 0) && (CurrentSelection < sels.Count) ? sels[CurrentSelection] : null;

			var currentSelection = default(Range);
			if (selecting ?? state.ShiftDown)
			{
				if (mouseRange != null)
				{
					sels.Remove(mouseRange);
					var anchor = mouseRange.Anchor;
					if (clickCount != 1)
					{
						if (position < anchor)
							position = GetPrevWord(position + 1);
						else
							position = GetNextWord(position);

						if ((mouseRange.Cursor <= anchor) != (position <= anchor))
						{
							if (position <= anchor)
								anchor = GetNextWord(anchor);
							else
								anchor = GetPrevWord(anchor);
						}
					}

					currentSelection = new Range(position, anchor);
				}
			}
			else
			{
				if (!state.ControlDown)
					sels.Clear();

				if (clickCount == 1)
					currentSelection = new Range(position);
				else
				{
					if (mouseRange != null)
						sels.Remove(mouseRange);
					currentSelection = new Range(GetNextWord(position), GetPrevWord(Math.Min(position + 1, Text.Length)));
				}
			}

			if (currentSelection != null)
				sels.Add(currentSelection);
			Selections = sels;
			if (currentSelection != null)
				CurrentSelection = Selections.FindIndex(currentSelection);
		}
	}
}
