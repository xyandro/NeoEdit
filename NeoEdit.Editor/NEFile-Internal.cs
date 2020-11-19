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
	partial class NEFile
	{
		void BlockSelDown()
		{
			var sels = new List<Range>();
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

				sels.Add(new Range(Text.GetPosition(cursorLine, cursorIndex), Text.GetPosition(highlightLine, highlightIndex)));
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
				var startLine = Text.GetPositionLine(range.Start);
				var endLine = Text.GetPositionLine(range.End);
				var startIndex = Text.GetPositionIndex(range.Start, startLine);
				var endIndex = Text.GetPositionIndex(range.End, endLine);

				startLine = Math.Max(0, Math.Min(startLine - 1, Text.NumLines - 1));
				endLine = Math.Max(0, Math.Min(endLine - 1, Text.NumLines - 1));
				startIndex = Math.Max(0, Math.Min(startIndex, Text.GetLineLength(startLine)));
				endIndex = Math.Max(0, Math.Min(endIndex, Text.GetLineLength(endLine)));

				var prevLineRange = new Range(Text.GetPosition(startLine, startIndex), Text.GetPosition(endLine, endIndex));
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
				var startLine = Text.GetPositionLine(range.Cursor);
				var startIndex = Text.GetPositionIndex(range.Cursor, startLine);

				if (lineRel)
					line = Text.SkipDiffGaps(line + startLine, line > 0 ? 1 : -1);
				if (indexRel)
					index += startIndex;
			}

			line = Math.Max(0, Math.Min(line, Text.NumLines - 1));
			index = Math.Max(0, Math.Min(index, Text.GetLineLength(line)));
			return MoveCursor(range, Text.GetPosition(line, index), selecting);
		}

		public static bool PreExecute_Internal_CommandLine()
		{
			var commandLineParams = (EditorExecuteState.CurrentState.Configuration as Configuration_Internal_CommandLine).CommandLineParams;
			if (commandLineParams.Background)
				return true;

			if (!commandLineParams.Files.Any())
			{
				new NEWindow(true);
				return true;
			}

			var shutdownData = string.IsNullOrWhiteSpace(commandLineParams.Wait) ? null : new ShutdownData(commandLineParams.Wait, commandLineParams.Files.Count);

			NEWindow neWindow = null;
			if (!commandLineParams.Diff)
				neWindow = EditorExecuteState.CurrentState.NEGlobal.NEWindows.OrderByDescending(x => x.LastActivated).FirstOrDefault();
			if (neWindow == null)
				neWindow = new NEWindow();
			foreach (var file in commandLineParams.Files)
			{
				if (commandLineParams.Existing)
				{
					var neFile = EditorExecuteState.CurrentState.NEGlobal.NEWindows.OrderByDescending(x => x.LastActivated).Select(x => x.GetFile(file.FileName)).NonNull().FirstOrDefault();
					if (neFile != null)
					{
						neFile.Goto(file.Line, file.Column, file.Index);
						continue;
					}
				}

				neWindow.AddNewFile(new NEFile(file.FileName, file.DisplayName, line: file.Line, column: file.Column, index: file.Index, shutdownData: shutdownData));
			}

			if (commandLineParams.Diff)
				neWindow.SetupDiff();

			neWindow.FilesWindow.SetForeground();

			return true;
		}

		static bool PreExecute_Internal_Activate()
		{
			EditorExecuteState.CurrentState.NEWindow.LastActivated = DateTime.Now;
			EditorExecuteState.CurrentState.NEWindow.AllFiles.ForEach(neFile => neFile.Activated());
			return true;
		}

		static bool PreExecute_Internal_MouseActivate()
		{
			var neFile = (EditorExecuteState.CurrentState.Configuration as Configuration_Internal_MouseActivate).NEFile as NEFile;
			EditorExecuteState.CurrentState.NEWindow.SetActiveFiles(EditorExecuteState.CurrentState.NEWindow.AllFiles.Where(file => (file == neFile) || ((EditorExecuteState.CurrentState.ShiftDown) && (EditorExecuteState.CurrentState.NEWindow.ActiveFiles.Contains(file)))));
			EditorExecuteState.CurrentState.NEWindow.Focused = neFile;
			return true;
		}

		static bool PreExecute_Internal_CloseFile()
		{
			var neFile = (EditorExecuteState.CurrentState.Configuration as Configuration_Internal_CloseFile).NEFile as NEFile;
			neFile.VerifyCanClose();
			neFile.ClearFiles();
			return true;
		}

		static void Configure_Internal_Key()
		{
			switch (EditorExecuteState.CurrentState.Key)
			{
				case Key.Back:
				case Key.Delete:
				case Key.Left:
				case Key.Right:
					if (EditorExecuteState.CurrentState.NEWindow.ActiveFiles.Any(neFile => neFile.Selections.Any(range => range.HasSelection)))
						EditorExecuteState.CurrentState.Configuration = new Configuration_Internal_Key { HasSelections = true };
					break;
			}
		}

		static bool PreExecute_Internal_Key()
		{
			if (!EditorExecuteState.CurrentState.ControlDown || EditorExecuteState.CurrentState.AltDown)
				return false;

			switch (EditorExecuteState.CurrentState.Key)
			{
				case Key.PageUp: EditorExecuteState.CurrentState.NEWindow.MovePrevNext(-1, EditorExecuteState.CurrentState.ShiftDown); break;
				case Key.PageDown: EditorExecuteState.CurrentState.NEWindow.MovePrevNext(1, EditorExecuteState.CurrentState.ShiftDown); break;
				case Key.Tab: EditorExecuteState.CurrentState.NEWindow.MovePrevNext(1, EditorExecuteState.CurrentState.ShiftDown, true); break;
				default: return false;
			}

			return true;
		}

		void Execute_Internal_Key()
		{
			var hasSelections = (EditorExecuteState.CurrentState.Configuration as Configuration_Internal_Key)?.HasSelections ?? false;
			switch (EditorExecuteState.CurrentState.Key)
			{
				case Key.Back:
				case Key.Delete:
					{
						if (hasSelections)
						{
							ReplaceSelections("");
							break;
						}

						Replace(Selections.AsTaskRunner().Select(range =>
						{
							var position = range.Start;
							var anchor = range.Anchor;

							if (EditorExecuteState.CurrentState.ControlDown)
							{
								if (EditorExecuteState.CurrentState.Key == Key.Back)
									position = GetPrevWord(position);
								else
									position = GetNextWord(position);
							}
							else if ((EditorExecuteState.CurrentState.ShiftDown) && (EditorExecuteState.CurrentState.Key == Key.Delete))
							{
								var line = Text.GetPositionLine(position);
								position = Text.GetPosition(line, 0);
								anchor = position + Text.GetLineLength(line) + Text.GetEndingLength(line);
							}
							else
							{
								var line = Text.GetPositionLine(position);
								var index = Text.GetPositionIndex(position, line);

								if (EditorExecuteState.CurrentState.Key == Key.Back)
									--index;
								else
									++index;

								if (index < 0)
								{
									--line;
									if (line < 0)
										return null;
									index = Text.GetLineLength(line);
								}
								if (index > Text.GetLineLength(line))
								{
									++line;
									if (line >= Text.NumLines)
										return null;
									index = 0;
								}

								position = Text.GetPosition(line, index);
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
						Execute_Edit_Select_Focused_Single();
						if (!Selections.Any())
							Selections = new List<Range> { new Range() };
					}
					break;
				case Key.Left:
					{
						Selections = Selections.AsTaskRunner().Select(range =>
						{
							if ((!EditorExecuteState.CurrentState.ShiftDown) && (hasSelections))
								return new Range(range.Start);

							var line = Text.GetPositionLine(range.Cursor);
							var index = Text.GetPositionIndex(range.Cursor, line);
							if ((index == 0) && (line != 0))
								return MoveCursor(range, range.Cursor - Math.Max(1, Text.GetEndingLength(line - 1)), EditorExecuteState.CurrentState.ShiftDown);
							else
								return MoveCursor(range, range.Cursor - 1, EditorExecuteState.CurrentState.ShiftDown);
						}).ToList();
					}
					break;
				case Key.Right:
					{
						Selections = Selections.AsTaskRunner().Select(range =>
						{
							if ((!EditorExecuteState.CurrentState.ShiftDown) && (hasSelections))
								return new Range(range.End);

							var line = Text.GetPositionLine(range.Cursor);
							var index = Text.GetPositionIndex(range.Cursor, line);
							if ((index == Text.GetLineLength(line)) && (line != Text.NumLines - 1))
								return MoveCursor(range, range.Cursor + Math.Max(1, Text.GetEndingLength(line)), EditorExecuteState.CurrentState.ShiftDown);
							else
								return MoveCursor(range, range.Cursor + 1, EditorExecuteState.CurrentState.ShiftDown);
						}).ToList();
					}
					break;
				case Key.Up:
				case Key.Down:
					{
						var mult = EditorExecuteState.CurrentState.Key == Key.Up ? -1 : 1;
						if (!EditorExecuteState.CurrentState.ControlDown)
							Selections = Selections.AsTaskRunner().Select(range => MoveCursor(range, mult, 0, EditorExecuteState.CurrentState.ShiftDown)).ToList();
						else if (!EditorExecuteState.CurrentState.ShiftDown)
							StartRow += mult;
						else if (EditorExecuteState.CurrentState.Key == Key.Down)
							BlockSelDown();
						else
							BlockSelUp();
					}
					break;
				case Key.Home:
					if (EditorExecuteState.CurrentState.ControlDown)
					{
						var sels = Selections.AsTaskRunner().Select(range => MoveCursor(range, 0, EditorExecuteState.CurrentState.ShiftDown)).ToList(); // Have to use MoveCursor for selection
						if ((!sels.Any()) && (!EditorExecuteState.CurrentState.ShiftDown))
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
							var line = Text.GetPositionLine(selection.Cursor);
							var index = Text.GetPositionIndex(selection.Cursor, line);

							int startText;
							var end = Text.GetLineLength(line);
							for (startText = 0; startText < end; ++startText)
							{
								if (!char.IsWhiteSpace(Text[Text.GetPosition(line, startText)]))
									break;
							}
							if (startText == end)
								startText = 0;

							if (startText != index)
								moveToStartText = true;
							startTextSels.Add(MoveCursor(selection, Text.GetPosition(line, startText), EditorExecuteState.CurrentState.ShiftDown));
							startLineSels.Add(MoveCursor(selection, Text.GetPosition(line, 0), EditorExecuteState.CurrentState.ShiftDown));
						}
						if (moveToStartText)
							Selections = startTextSels;
						else
							Selections = startLineSels;
					}
					break;
				case Key.End:
					if (EditorExecuteState.CurrentState.ControlDown)
					{
						var sels = Selections.AsTaskRunner().Select(range => MoveCursor(range, Text.Length, EditorExecuteState.CurrentState.ShiftDown)).ToList(); // Have to use MoveCursor for selection
						if ((!sels.Any()) && (!EditorExecuteState.CurrentState.ShiftDown))
							sels = new List<Range> { new Range(Text.Length) };
						Selections = sels;
					}
					else
						Selections = Selections.AsTaskRunner().Select(range => MoveCursor(range, 0, int.MaxValue, EditorExecuteState.CurrentState.ShiftDown, indexRel: false)).ToList();
					break;
				case Key.PageUp:
					if (EditorExecuteState.CurrentState.ControlDown)
						StartRow -= EditorExecuteState.CurrentState.NEWindow.DisplayRows / 2;
					else
						Selections = Selections.AsTaskRunner().Select(range => MoveCursor(range, 1 - EditorExecuteState.CurrentState.NEWindow.DisplayRows, 0, EditorExecuteState.CurrentState.ShiftDown)).ToList();
					break;
				case Key.PageDown:
					if (EditorExecuteState.CurrentState.ControlDown)
						StartRow += EditorExecuteState.CurrentState.NEWindow.DisplayRows / 2;
					else
						Selections = Selections.AsTaskRunner().Select(range => MoveCursor(range, EditorExecuteState.CurrentState.NEWindow.DisplayRows - 1, 0, EditorExecuteState.CurrentState.ShiftDown)).ToList();
					break;
				case Key.Tab:
					{
						if (Selections.AsTaskRunner().All(range => (!range.HasSelection) || (Text.GetPositionLine(range.Start) == Text.GetPositionLine(Math.Max(range.Start, range.End - 1)))))
						{
							if (!EditorExecuteState.CurrentState.ShiftDown)
								ReplaceSelections("\t", false, tryJoinUndo: true);
							else
							{
								var neWindow = Selections.AsTaskRunner().Where(range => (range.Start != 0) && (Text.GetString(range.Start - 1, 1) == "\t")).Select(range => Range.FromIndex(range.Start - 1, 1)).ToList();
								Replace(neWindow, Enumerable.Repeat("", neWindow.Count).ToList());
							}
							break;
						}

						var selLines = Selections.AsTaskRunner().Where(a => a.HasSelection).Select(range => new { start = Text.GetPositionLine(range.Start), end = Text.GetPositionLine(range.End - 1) }).ToList();
						var lines = selLines.SelectMany(entry => Enumerable.Range(entry.start, entry.end - entry.start + 1)).Distinct().OrderBy().ToDictionary(line => line, line => Text.GetPosition(line, 0));
						int length;
						string replace;
						if (EditorExecuteState.CurrentState.ShiftDown)
						{
							length = 1;
							replace = "";
							lines = lines.Where(entry => (Text.GetLineLength(entry.Key) != 0) && (Text[Text.GetPosition(entry.Key, 0)] == '\t')).ToDictionary(entry => entry.Key, entry => entry.Value);
						}
						else
						{
							length = 0;
							replace = "\t";
							lines = lines.Where(entry => Text.GetLineLength(entry.Key) != 0).ToDictionary(entry => entry.Key, entry => entry.Value);
						}

						var sels = lines.Select(line => Range.FromIndex(line.Value, length)).ToList();
						var insert = sels.Select(range => replace).ToList();
						Replace(sels, insert);
					}
					break;
				case Key.Enter:
					ReplaceSelections(Text.DefaultEnding, false, tryJoinUndo: true);
					break;
				default: throw new OperationCanceledException();
			}
		}

		void Execute_Internal_Text() => ReplaceSelections(EditorExecuteState.CurrentState.Text, false, tryJoinUndo: true);

		void Execute_Internal_SetBinaryValue()
		{
			var configuration = EditorExecuteState.CurrentState.Configuration as Configuration_Internal_SetBinaryValue;
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

		static bool PreExecute_Internal_Scroll()
		{
			var configuration = EditorExecuteState.CurrentState.Configuration as Configuration_Internal_Scroll;
			var neFile = configuration.NEFile as NEFile;
			neFile.StartColumn = configuration.Column;
			neFile.StartRow = configuration.Row;
			return true;
		}

		static bool PreExecute_Internal_Mouse()
		{
			var configuration = EditorExecuteState.CurrentState.Configuration as Configuration_Internal_Mouse;
			var neFile = configuration.NEFile as NEFile;

			if ((EditorExecuteState.CurrentState.NEWindow.ActiveFiles.Count != 1) || (!EditorExecuteState.CurrentState.NEWindow.ActiveFiles.Contains(neFile)))
			{
				EditorExecuteState.CurrentState.NEWindow.SetActiveFile(neFile);
				return true;
			}

			neFile.Execute_Internal_Mouse(configuration.Line, configuration.Column, configuration.ClickCount, configuration.Selecting);

			return true;
		}

		public void Execute_Internal_Mouse(int line, int column, int clickCount, bool? selecting)
		{
			var sels = Selections.ToList();
			line = Math.Max(0, Math.Min(line, Text.NumLines - 1));
			column = Math.Max(0, Math.Min(column, Text.GetLineColumnsLength(line)));
			var index = Text.GetIndexFromColumn(line, column, true);
			var position = Text.GetPosition(line, index);
			var mouseRange = (CurrentSelection >= 0) && (CurrentSelection < sels.Count) ? sels[CurrentSelection] : null;

			var currentSelection = default(Range);
			if (selecting ?? EditorExecuteState.CurrentState.ShiftDown)
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
				if (!EditorExecuteState.CurrentState.ControlDown)
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
