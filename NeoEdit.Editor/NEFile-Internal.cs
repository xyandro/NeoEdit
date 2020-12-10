using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Transform;
using NeoEdit.Editor.PreExecution;
using NeoEdit.TaskRunning;

namespace NeoEdit.Editor
{
	partial class NEFile
	{
		void BlockSelDown()
		{
			var sels = new List<NERange>();
			foreach (var range in Selections)
			{
				var cursorLine = Text.GetPositionLine(range.Cursor);
				var highlightLine = Text.GetPositionLine(range.Anchor);
				var cursorIndex = Text.GetPositionIndex(range.Cursor, cursorLine);
				var highlightIndex = Text.GetPositionIndex(range.Anchor, highlightLine);

				cursorLine = Math.Max(0, Math.Min(cursorLine + 1, Text.NumLines - 1));
				highlightLine = Math.Max(0, Math.Min(highlightLine + 1, Text.NumLines - 1));
				cursorIndex = Math.Max(0, Math.Min(cursorIndex, Text.GetLineLength(cursorLine)));
				highlightIndex = Math.Max(0, Math.Min(highlightIndex, Text.GetLineLength(highlightLine)));

				sels.Add(new NERange(Text.GetPosition(highlightLine, highlightIndex), Text.GetPosition(cursorLine, cursorIndex)));
			}
			Selections = Selections.Concat(sels).ToList();
		}

		void BlockSelUp()
		{
			var found = new HashSet<string>();
			foreach (var range in Selections)
				found.Add(range.ToString());

			var sels = new List<NERange>();
			foreach (var range in Selections)
			{
				var startLine = Text.GetPositionLine(range.Start);
				var endLine = Text.GetPositionLine(range.End);
				var startIndex = Text.GetPositionIndex(range.Start, startLine);
				var endIndex = Text.GetPositionIndex(range.End, endLine);

				startLine = Math.Max(0, Math.Min(startLine - 1, Text.NumLines - 1));
				endLine = Math.Max(0, Math.Min(endLine - 1, Text.NumLines - 1));
				startIndex = Math.Max(0, Math.Min(startIndex, Text.GetLineLength(startLine)));
				endIndex = Math.Max(0, Math.Min(endIndex, Text.GetLineLength(endLine)));

				var prevLineRange = new NERange(Text.GetPosition(endLine, endIndex), Text.GetPosition(startLine, startIndex));
				if (found.Contains(prevLineRange.ToString()))
					sels.Add(prevLineRange);
				else
					sels.Add(range);
			}

			Selections = sels;
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

		NERange MoveCursor(NERange range, int position, bool selecting)
		{
			position = Math.Max(0, Math.Min(position, Text.Length));
			if (selecting)
				if (range.Cursor == position)
					return range;
				else
					return new NERange(range.Anchor, position);

			if ((range.Cursor == position) && (range.Anchor == position))
				return range;
			return new NERange(position);
		}

		void Execute_Internal_Key()
		{
			switch (state.Key)
			{
				case Key.Back: case Key.Delete: Execute_Internal_Key_BackDelete(); break;
				case Key.Escape: Execute_Internal_Key_Escape(); break;
				case Key.Left: Execute_Internal_Key_Left(); break;
				case Key.Right: Execute_Internal_Key_Right(); break;
				case Key.Up: Execute_Internal_Key_Up(); break;
				case Key.Down: Execute_Internal_Key_Down(); break;
				case Key.Home: Execute_Internal_Key_Home(); break;
				case Key.End: Execute_Internal_Key_End(); break;
				case Key.PageUp: Execute_Internal_Key_PageUp(); break;
				case Key.PageDown: Execute_Internal_Key_PageDown(); break;
				case Key.Tab: Execute_Internal_Key_Tab(); break;
				case Key.Enter: Execute_Internal_Key_Enter(); break;
			}
		}

		void Execute_Internal_Key_BackDelete()
		{
			if ((state.Configuration as Configuration_Internal_Key)?.HasSelections == true)
			{
				ReplaceSelections("");
				return;
			}

			Replace(Selections.AsTaskRunner().Select(range =>
			{
				var anchor = range.Anchor;
				var cursor = range.Cursor;

				if (state.ControlDown)
				{
					if (state.Key == Key.Back)
						cursor = GetPrevWord(cursor);
					else
						cursor = GetNextWord(cursor);
				}
				else if ((state.ShiftDown) && (state.Key == Key.Delete))
				{
					anchor = Text.GetLineStartPosition(cursor);
					cursor = Text.NextChar(Text.GetLineEndPosition(cursor), true);
				}
				else if (state.Key == Key.Back)
					cursor = Text.PrevChar(cursor, true);
				else
					cursor = Text.NextChar(cursor, true);

				return new NERange(anchor, cursor);
			}).Where(x => x.HasSelection).ToList());
		}

		void Execute_Internal_Key_Escape()
		{
			if (ViewBinarySearches != null)
			{
				ViewBinarySearches = null;
				return;
			}
			//DragFiles = null;
			if (Settings.EscapeClearsSelections)
			{
				Execute_Edit_Select_Focused_Single();
				if (!Selections.Any())
					Selections = new List<NERange> { new NERange() };
			}
		}

		void Execute_Internal_Key_Left()
		{
			if ((!state.ShiftDown) && ((state.Configuration as Configuration_Internal_Key)?.HasSelections == true))
			{
				Selections = Selections.AsTaskRunner().Select(range => new NERange(range.Start)).ToList();
				return;
			}

			Selections = Selections.AsTaskRunner().Select(range =>
			{
				var cursor = range.Cursor;
				var anchor = range.Anchor;
				cursor = Text.PrevChar(cursor, true);
				if (!state.ShiftDown)
					anchor = cursor;
				return new NERange(anchor, cursor);
			}).ToList();
		}

		void Execute_Internal_Key_Right()
		{
			if ((!state.ShiftDown) && ((state.Configuration as Configuration_Internal_Key)?.HasSelections == true))
			{
				Selections = Selections.AsTaskRunner().Select(range => new NERange(range.End)).ToList();
				return;
			}

			Selections = Selections.AsTaskRunner().Select(range =>
			{
				var cursor = range.Cursor;
				var anchor = range.Anchor;
				cursor = Text.NextChar(cursor, true);
				if (!state.ShiftDown)
					anchor = cursor;
				return new NERange(anchor, cursor);
			}).ToList();
		}

		void Execute_Internal_Key_Up()
		{
			if ((!state.ShiftDown) && ((state.Configuration as Configuration_Internal_Key)?.HasSelections == true))
			{
				Selections = Selections.AsTaskRunner().Select(range => new NERange(range.Start)).ToList();
				return;
			}

			if (!state.ControlDown)
			{
				Selections = Selections.AsTaskRunner().Select(range =>
				{
					var anchor = range.Anchor;
					var cursor = range.Cursor;
					var lineStart = Text.GetLineStartPosition(cursor);
					if (lineStart != 0)
					{
						var column = Text.GetColumnFromPosition(cursor, lineStart);
						var prevLineStart = Text.GetLineStartPosition(Text.PrevChar(lineStart));
						cursor = Text.GetPositionFromColumn(column, prevLineStart, true);
					}
					if (!state.ShiftDown)
						anchor = cursor;
					return new NERange(anchor, cursor);
				}).ToList();
			}
			else if (!state.ShiftDown)
				StartRow += -1;
			else if (state.Key == Key.Down)
				BlockSelDown();
			else
				BlockSelUp();
		}

		void Execute_Internal_Key_Down()
		{
			if ((!state.ShiftDown) && ((state.Configuration as Configuration_Internal_Key)?.HasSelections == true))
			{
				Selections = Selections.AsTaskRunner().Select(range => new NERange(range.End)).ToList();
				return;
			}

			if (!state.ControlDown)
			{
				Selections = Selections.AsTaskRunner().Select(range =>
				{
					var anchor = range.Anchor;
					var cursor = range.Cursor;
					var lineEnd = Text.GetLineEndPosition(cursor);
					if (lineEnd != Text.Length)
					{
						var lineStart = Text.GetLineStartPosition(cursor);
						var column = Text.GetColumnFromPosition(cursor, lineStart);

						var nextLineStart = Text.GetLineStartPosition(Text.NextChar(lineEnd));
						cursor = Text.GetPositionFromColumn(column, nextLineStart, true);
					}
					if (!state.ShiftDown)
						anchor = cursor;
					return new NERange(anchor, cursor);
				}).ToList();
			}
			else if (!state.ShiftDown)
				StartRow += 1;
			else if (state.Key == Key.Down)
				BlockSelDown();
			else
				BlockSelUp();
		}

		void Execute_Internal_Key_Home()
		{
			if (state.ControlDown)
				Selections = Selections.AsTaskRunner().Select(range => new NERange(state.ShiftDown ? range.Anchor : 0, 0)).ToList();
			else
			{
				var moveToStartText = false;
				var datas = Selections.AsTaskRunner().Select(range =>
				{
					var startLine = Text.GetLineStartPosition(range.Cursor);

					var startText = startLine;
					while (true)
					{
						if (startText >= Text.Length)
						{
							startText = startLine;
							break;
						}
						if (!char.IsWhiteSpace(Text[startText]))
							break;
						if ((Text[startText] == '\r') || (Text[startText] == '\n'))
						{
							startText = startLine;
							break;
						}
						++startText;
					}

					if (range.Cursor != startText)
						moveToStartText = true;

					return (startLine, startText);
				}).ToList();

				if (moveToStartText)
					Selections = datas.Select((data, index) => new NERange(state.ShiftDown ? Selections[index].Anchor : data.startText, data.startText)).ToList();
				else
					Selections = datas.Select((data, index) => new NERange(state.ShiftDown ? Selections[index].Anchor : data.startLine, data.startLine)).ToList();
			}
		}

		void Execute_Internal_Key_End()
		{
			if (state.ControlDown)
				Selections = Selections.AsTaskRunner().Select(range => new NERange(state.ShiftDown ? range.Anchor : Text.Length, Text.Length)).ToList();
			else
				Selections = Selections.AsTaskRunner().Select(range =>
				{
					var anchor = range.Anchor;
					var cursor = range.Cursor;
					cursor = Text.GetLineEndPosition(cursor);
					if (!state.ShiftDown)
						anchor = cursor;
					return new NERange(anchor, cursor);
				}).ToList();
		}

		void Execute_Internal_Key_PageUp()
		{
			Selections = Selections.AsTaskRunner().Select(range =>
			{
				var anchor = range.Anchor;
				var cursor = range.Cursor;
				var lineStart = Text.GetLineStartPosition(cursor);
				var column = Text.GetColumnFromPosition(cursor, lineStart);
				for (var ctr = 0; ctr < NEWindow.DisplayRows - 1; ++ctr)
					if (lineStart == 0)
						break;
					else
						lineStart = Text.GetLineStartPosition(Text.PrevChar(lineStart));
				cursor = Text.GetPositionFromColumn(column, lineStart, true);
				if (!state.ShiftDown)
					anchor = cursor;
				return new NERange(anchor, cursor);
			}).ToList();
		}

		void Execute_Internal_Key_PageDown()
		{
			Selections = Selections.AsTaskRunner().Select(range =>
			{
				var anchor = range.Anchor;
				var cursor = range.Cursor;
				var lineStart = Text.GetLineStartPosition(cursor);
				var column = Text.GetColumnFromPosition(cursor, lineStart);
				for (var ctr = 0; ctr < NEWindow.DisplayRows - 1; ++ctr)
					if (lineStart == Text.Length)
						break;
					else
						lineStart = Text.NextChar(Text.GetLineEndPosition(lineStart), true);
				cursor = Text.GetPositionFromColumn(column, lineStart, true);
				if (!state.ShiftDown)
					anchor = cursor;
				return new NERange(anchor, cursor);
			}).ToList();
		}

		void Execute_Internal_Key_Tab()
		{
			if (Selections.AsTaskRunner().All(range => (!range.HasSelection) || (Text.GetLineStartPosition(range.Start) == Text.GetLineStartPosition(range.End))))
			{
				ReplaceSelections("\t", false);
				return;
			}

			var datas = Selections.AsTaskRunner().Select(range =>
			{
				var start = Text.GetLineStartPosition(range.Start);
				var lineStarts = new List<int>();
				while (true)
				{
					while ((start < range.Start) && (Text[start] == '\t'))
						++start;

					var end = Text.NextChar(Text.GetLineEndPosition(start), true);
					lineStarts.Add(start);

					start = end;
					if (start >= range.End)
						break;
				}
				return (range, lineStarts);
			}).ToList();

			var offset = 0;
			var replaceRanges = new List<NERange>();
			var replaceStrs = new List<string>();
			var newSels = new List<NERange>();
			foreach (var data in datas)
			{
				var rangeStart = data.range.Start + offset;
				var rangeEnd = data.range.End + offset;
				foreach (var _lineStart in data.lineStarts)
				{
					var lineStart = _lineStart;

					if (state.ShiftDown)
					{
						if ((lineStart >= Text.Length) || (Text[lineStart] != '\t'))
						{
							if ((lineStart == 0) || (Text[lineStart - 1] != '\t'))
								continue;
							--lineStart;
						}
						replaceRanges.Add(NERange.FromIndex(lineStart, 1));
						replaceStrs.Add("");
						--offset;
						if (data.range.Start > lineStart)
							--rangeStart;
						if (data.range.End > lineStart)
							--rangeEnd;
					}
					else
					{
						replaceRanges.Add(NERange.FromIndex(lineStart, 0));
						replaceStrs.Add("\t");
						++offset;
						if (data.range.Start > lineStart)
							++rangeStart;
						++rangeEnd;
					}

				}
				newSels.Add(new NERange(rangeStart, rangeEnd));
			}

			Replace(replaceRanges, replaceStrs);
			Selections = newSels;
		}

		void Execute_Internal_Key_Enter() => ReplaceSelections(Text.DefaultEnding, false);

		void Execute_Internal_Text() => ReplaceSelections(state.Text, false);

		void Execute_Internal_SetBinaryValue()
		{
			var configuration = state.Configuration as Configuration_Internal_SetBinaryValue;
			var newStr = Coder.BytesToString(configuration.Value, CodePage);
			var replaceRange = Selections[CurrentSelection];
			var newSels = new List<NERange>();
			var offset = 0;
			foreach (var range1 in Selections)
			{
				var range = range1;
				if (range == replaceRange)
				{
					replaceRange = NERange.FromIndex(replaceRange.Start, configuration.OldSize ?? replaceRange.Length);
					offset += newStr.Length - replaceRange.Length;
					range = NERange.FromIndex(range.Start, range.Length + offset);
				}
				else
					range = range.Move(offset);
				newSels.Add(range);
			}
			Replace(new List<NERange> { replaceRange }, new List<string> { newStr });
			Selections = newSels;
		}

		static void PreExecute_Internal_Scroll()
		{
			var configuration = state.Configuration as Configuration_Internal_Scroll;
			var neFile = configuration.NEFile as NEFile;
			neFile.StartColumn = configuration.Column;
			neFile.StartRow = configuration.Row;
			state.PreExecution = PreExecution_TaskFinished.Singleton;
		}

		public void Execute_Internal_Mouse(int line, int column, int clickCount, bool selecting)
		{
			var sels = Selections.ToList();
			line = Math.Max(0, Math.Min(line, Text.NumLines - 1));
			column = Math.Max(0, Math.Min(column, Text.GetLineColumnsLength(line)));
			var index = Text.GetIndexFromColumn(line, column, true);
			var position = Text.GetPosition(line, index);
			var mouseRange = (CurrentSelection >= 0) && (CurrentSelection < sels.Count) ? sels[CurrentSelection] : null;

			var currentSelection = default(NERange);
			if ((selecting) || (state.ShiftDown))
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

					currentSelection = new NERange(anchor, position);
				}
			}
			else
			{
				if (!state.ControlDown)
					sels.Clear();

				if (clickCount == 1)
					currentSelection = new NERange(position);
				else
				{
					if (mouseRange != null)
						sels.Remove(mouseRange);
					currentSelection = new NERange(GetPrevWord(Math.Min(position + 1, Text.Length)), GetNextWord(position));
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
