﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using NeoEdit.Common;
using NeoEdit.Common.Transform;
using NeoEdit.GUI;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Dialogs;
using NeoEdit.TextEditor.Dialogs;

namespace NeoEdit.TextEditor
{
	partial class TextCanvas : Canvas
	{
		[DepProp]
		internal TextData Data { get { return uiHelper.GetPropValue<TextData>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		internal Highlighting.HighlightingType HighlightType { get { return uiHelper.GetPropValue<Highlighting.HighlightingType>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		bool HasBOM { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int Line { get { return uiHelper.GetPropValue<int>(); } private set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int Column { get { return uiHelper.GetPropValue<int>(); } private set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int Index { get { return uiHelper.GetPropValue<int>(); } private set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int NumSelections { get { return uiHelper.GetPropValue<int>(); } private set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int xScrollMaximum { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int xScrollSmallChange { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int xScrollLargeChange { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int xScrollValue { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int yScrollMaximum { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int yScrollSmallChange { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int yScrollLargeChange { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int yScrollValue { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }

		const int tabStop = 4;
		readonly double charWidth;
		readonly double lineHeight;

		int numLines { get { return (int)(ActualHeight / lineHeight); } }
		int numColumns { get { return (int)(ActualWidth / charWidth); } }

		enum RangeType
		{
			Search,
			Mark,
			Selection,
		}

		class Range
		{
			public Range Copy()
			{
				return new Range { Pos1 = Pos1, Pos2 = Pos2 };
			}

			public int Pos1 { get; set; }
			public int Pos2 { get; set; }
			public int Start { get { return Math.Min(Pos1, Pos2); } }
			public int End { get { return Math.Max(Pos1, Pos2); } }
			public int Length { get { return Math.Abs(Pos1 - Pos2); } }

			public bool HasSelection()
			{
				return Pos1 != Pos2;
			}

			public override string ToString()
			{
				return String.Format("({0:0000000000})->({1:0000000000})", Start, End);
			}
		}

		Dictionary<RangeType, List<Range>> ranges = Helpers.GetValues<RangeType>().ToDictionary(rangeType => rangeType, rangeType => new List<Range>());
		static Dictionary<string, string> keysToValues = new Dictionary<string, string>();

		readonly Typeface typeface;
		readonly double fontSize;

		class TextCanvasUndoRedo
		{
			public List<Range> ranges { get; private set; }
			public List<string> text { get; private set; }

			public TextCanvasUndoRedo(List<Range> _ranges, List<string> _text)
			{
				ranges = _ranges;
				text = _text;
			}
		}

		List<TextCanvasUndoRedo> undo = new List<TextCanvasUndoRedo>();
		List<TextCanvasUndoRedo> redo = new List<TextCanvasUndoRedo>();

		static TextCanvas() { UIHelper<TextCanvas>.Register(); }

		readonly UIHelper<TextCanvas> uiHelper;
		public TextCanvas()
		{
			uiHelper = new UIHelper<TextCanvas>(this);
			InitializeComponent();

			var fontFamily = new FontFamily(new Uri("pack://application:,,,/GUI;component/"), "./Resources/#Anonymous Pro");
			typeface = fontFamily.GetTypefaces().First();
			fontSize = 14;
			lineHeight = fontSize;

			var example = "0123456789 abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ !@#$%^&*()";
			var formattedText = new FormattedText(example, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
			charWidth = formattedText.Width / example.Length;

			uiHelper.AddCallback(a => a.Data, (o, n) =>
			{
				foreach (var entry in ranges)
					ranges[entry.Key].Clear();
				InvalidateVisual();
				undo.Clear();
				redo.Clear();
			});
			uiHelper.AddCallback(a => a.xScrollValue, (o, n) => InvalidateVisual());
			uiHelper.AddCallback(a => a.yScrollValue, (o, n) => InvalidateVisual());
			uiHelper.AddCallback(a => a.HighlightType, (o, n) => InvalidateVisual());
			uiHelper.AddCallback(Canvas.ActualWidthProperty, this, () => InvalidateVisual());
			uiHelper.AddCallback(Canvas.ActualHeightProperty, this, () => InvalidateVisual());

			Loaded += (s, e) =>
			{
				Line = 0;
				InvalidateVisual();
			};
		}

		public void GotoPos(int line, int column)
		{
			try
			{
				var index = GetIndexFromColumn(line - 1, column - 1);
				var range = new Range();
				ranges[RangeType.Selection].Clear();
				ranges[RangeType.Selection].Add(range);
				SetPos1(range, line - 1, index, false, false);
			}
			catch { }
		}

		void EnsureVisible(Range selection)
		{
			var line = Data.GetOffsetLine(selection.Pos1);
			var index = Data.GetOffsetIndex(selection.Pos1, line);
			yScrollValue = Math.Min(line, Math.Max(line - numLines + 1, yScrollValue));
			var x = GetXFromLineIndex(line, index);
			xScrollValue = Math.Min(x, Math.Max(x - numColumns + 1, xScrollValue));
		}

		int GetXFromLineIndex(int line, int index)
		{
			return GetColumnFromIndex(line, index);
		}

		int GetColumnFromIndex(int line, int index)
		{
			return GetColumnFromIndex(Data[line], index);
		}

		int GetColumnFromIndex(string lineStr, int findIndex)
		{
			if ((findIndex < 0) || (findIndex > lineStr.Length))
				throw new IndexOutOfRangeException();

			var column = 0;
			var index = 0;
			while (index < findIndex)
			{
				var find = lineStr.IndexOf('\t', index);
				if (find == index)
				{
					column = (column / tabStop + 1) * tabStop;
					++index;
					continue;
				}

				if (find == -1)
					find = lineStr.Length;
				find = Math.Min(find, findIndex);

				column += find - index;
				index = find;
			}
			return column;
		}

		int GetIndexFromColumn(int line, int column)
		{
			return GetIndexFromColumn(Data[line], column);
		}

		int GetIndexFromColumn(string lineStr, int findColumn)
		{
			if (findColumn < 0)
				throw new IndexOutOfRangeException();

			var column = 0;
			var index = 0;
			while (index < lineStr.Length)
			{
				if (column >= findColumn)
					break;

				var find = lineStr.IndexOf('\t', index);
				if (find == index)
				{
					column = (column / tabStop + 1) * tabStop;
					++index;
					continue;
				}

				if (find == -1)
					find = lineStr.Length;
				find = Math.Min(find, findColumn - column + index);

				column += find - index;
				index = find;
			}
			return index;
		}

		DispatcherTimer drawTimer = null;
		new void InvalidateVisual()
		{
			if (drawTimer != null)
				return;

			drawTimer = new DispatcherTimer();
			drawTimer.Tick += (s, e) =>
			{
				drawTimer.Stop();
				drawTimer = null;

				base.InvalidateVisual();
			};
			drawTimer.Start();
		}

		static Dictionary<RangeType, Brush> brushes = new Dictionary<RangeType, Brush>
		{
			{ RangeType.Selection, new SolidColorBrush(Color.FromArgb(128, 58, 143, 205)) }, //9cc7e6
			{ RangeType.Search, new SolidColorBrush(Color.FromArgb(128, 197, 205, 173)) }, //e2e6d6
			{ RangeType.Mark, new SolidColorBrush(Color.FromArgb(178, 242, 155, 0)) }, //f6b94d
		};
		protected override void OnRender(DrawingContext dc)
		{
			base.OnRender(dc);

			if (Data == null)
				return;

			HasBOM = Data.BOM;
			var columns = Enumerable.Range(0, Data.NumLines).Select(lineNum => Data[lineNum]).Select(line => GetColumnFromIndex(line, line.Length)).Max();

			xScrollMaximum = columns - numColumns;
			xScrollSmallChange = 1;
			xScrollLargeChange = numColumns - xScrollSmallChange;

			yScrollMaximum = Data.NumLines - numLines;
			yScrollSmallChange = 1;
			yScrollLargeChange = numLines - yScrollSmallChange;

			if (ranges[RangeType.Selection].Count == 0)
			{
				var range = new Range();
				ranges[RangeType.Selection].Add(range);
				SetPos1(range, 0, 0, false, false);
			}
			ranges[RangeType.Search] = ranges[RangeType.Search].Where(range => range.HasSelection()).ToList();

			EnsureVisible(ranges[RangeType.Selection].First());

			var keys = ranges.Keys.ToList();
			foreach (var key in keys)
			{
				ranges[key] = ranges[key].GroupBy(range => range.ToString()).Select(rangeGroup => rangeGroup.First()).OrderBy(range => range.Start).ToList();
				// Make sure ranges don't overlap
				for (var ctr = 0; ctr < ranges[key].Count - 1; ctr++)
				{
					ranges[key][ctr].Pos1 = Math.Min(ranges[key][ctr].Pos1, ranges[key][ctr + 1].Start);
					ranges[key][ctr].Pos2 = Math.Min(ranges[key][ctr].Pos2, ranges[key][ctr + 1].Start);
				}
			}

			var pos = ranges[RangeType.Selection].First().Pos1;
			Line = Data.GetOffsetLine(pos) + 1;
			Index = Data.GetOffsetIndex(pos, Line - 1) + 1;
			Column = GetColumnFromIndex(Line - 1, Index - 1) + 1;
			NumSelections = ranges[RangeType.Selection].Count;

			var startLine = yScrollValue;
			var endLine = Math.Min(Data.NumLines, startLine + numLines + 1);

			var startChar = xScrollValue;
			var endChar = Math.Min(columns, startChar + numColumns + 1);

			var highlightDictionary = Highlighting.Get(HighlightType).GetDictionary();

			for (var line = startLine; line < endLine; ++line)
			{
				var lineStr = Data[line];
				var lineRange = new Range { Pos1 = Data.GetOffset(line, 0), Pos2 = Data.GetOffset(line, lineStr.Length) };
				var y = (line - startLine) * lineHeight;

				foreach (var entry in ranges)
				{
					foreach (var range in entry.Value)
					{
						if ((range.End < lineRange.Start) || (range.Start > lineRange.End))
							continue;

						var start = Math.Max(lineRange.Start, range.Start);
						var end = Math.Min(lineRange.End, range.End);
						start = Data.GetOffsetIndex(start, line);
						end = Data.GetOffsetIndex(end, line);
						start = GetColumnFromIndex(lineStr, start);
						end = GetColumnFromIndex(lineStr, end);
						if (range.End > lineRange.End)
							end++;

						if ((start >= endChar) || (end < startChar))
							continue;

						start = Math.Max(0, start - startChar);
						end = Math.Min(endChar, end) - startChar;
						var width = end - start;

						dc.DrawRectangle(brushes[entry.Key], null, new Rect(start * charWidth, y, width * charWidth + 1, lineHeight));
					}
				}

				foreach (var selection in ranges[RangeType.Selection])
				{
					if ((selection.Pos1 < lineRange.Start) || (selection.Pos1 > lineRange.End))
						continue;

					var column = GetColumnFromIndex(lineStr, Data.GetOffsetIndex(selection.Pos1, line));
					if ((column < startChar) || (column > endChar))
						continue;
					dc.DrawRectangle(Brushes.Black, null, new Rect((column - startChar) * charWidth, y, 1, lineHeight));
				}

				var index = 0;
				var sb = new StringBuilder();
				while (index < lineStr.Length)
				{
					var find = lineStr.IndexOf('\t', index);
					if (find == index)
					{
						sb.Append(' ', (sb.Length / tabStop + 1) * tabStop - sb.Length);
						++index;
						continue;
					}

					if (find == -1)
						find = lineStr.Length;
					sb.Append(lineStr, index, find - index);
					index = find;
				}

				var str = sb.ToString();
				if (str.Length <= startChar)
					continue;

				var highlight = new List<Tuple<Brush, int, int>>();
				foreach (var entry in highlightDictionary)
				{
					var matches = entry.Key.Matches(str);
					foreach (Match match in matches)
						highlight.Add(new Tuple<Brush, int, int>(entry.Value, match.Index, match.Length));
				}

				str = str.Substring(startChar, Math.Min(endChar, str.Length) - startChar);
				var text = new FormattedText(str, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
				foreach (var entry in highlight)
				{
					var start = entry.Item2 - startChar;
					var count = entry.Item3;
					if (start < 0)
					{
						count += start;
						start = 0;
					}
					count = Math.Min(count, str.Length - start);
					if (count <= 0)
						continue;
					text.SetForegroundBrush(entry.Item1, start, count);
				}
				dc.DrawText(text, new Point(0, y));
			}
		}

		bool mouseDown;
		bool? shiftOverride;
		bool shiftDown { get { return shiftOverride.HasValue ? shiftOverride.Value : (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None; } }
		bool controlDown { get { return (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None; } }
		bool altOnly { get { return (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt)) == ModifierKeys.Alt; } }
		bool selecting { get { return (mouseDown) || (shiftDown); } }

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.Handled)
				return;

			e.Handled = true;
			switch (e.Key)
			{
				case Key.Back:
				case Key.Delete:
					foreach (var selection in ranges[RangeType.Selection])
					{
						if (selection.HasSelection())
							continue;

						var line = Data.GetOffsetLine(selection.Start);
						var index = Data.GetOffsetIndex(selection.Start, line);

						if (e.Key == Key.Back)
							--index;
						else
							++index;

						if (index < 0)
						{
							--line;
							if (line < 0)
								continue;
							index = Data[line].Length;
						}
						if (index > Data[line].Length)
						{
							++line;
							if (line >= Data.NumLines)
								continue;
							index = 0;
						}

						selection.Pos1 = Data.GetOffset(line, index);
					}

					Replace(ranges[RangeType.Selection], null, false);
					break;
				case Key.Escape:
					ranges[RangeType.Search].Clear();
					InvalidateVisual();
					break;
				case Key.Left:
					foreach (var selection in ranges[RangeType.Selection])
					{
						var line = Data.GetOffsetLine(selection.Pos1);
						var index = Data.GetOffsetIndex(selection.Pos1, line);
						if (controlDown)
							MovePrevWord(selection);
						else if ((index == 0) && (line != 0))
							SetPos1(selection, -1, Int32.MaxValue, indexRel: false);
						else
							SetPos1(selection, 0, -1);
					}
					break;
				case Key.Right:
					foreach (var selection in ranges[RangeType.Selection])
					{
						var line = Data.GetOffsetLine(selection.Pos1);
						var index = Data.GetOffsetIndex(selection.Pos1, line);
						if (controlDown)
							MoveNextWord(selection);
						else if ((index == Data[line].Length) && (line != Data.NumLines - 1))
							SetPos1(selection, 1, 0, indexRel: false);
						else
							SetPos1(selection, 0, 1);
					}
					break;
				case Key.Up:
				case Key.Down:
					{
						var mult = e.Key == Key.Up ? -1 : 1;
						if (controlDown)
							yScrollValue += mult;
						else
							foreach (var selection in ranges[RangeType.Selection])
								SetPos1(selection, mult, 0);
					}
					break;
				case Key.Home:
					if (controlDown)
					{
						foreach (var selection in ranges[RangeType.Selection])
							SetPos1(selection, 0, 0, false, false);
					}
					else
					{
						bool changed = false;
						foreach (var selection in ranges[RangeType.Selection])
						{
							var line = Data.GetOffsetLine(selection.Pos1);
							var index = Data.GetOffsetIndex(selection.Pos1, line);
							int tmpIndex;
							var lineStr = Data[line];
							for (tmpIndex = 0; tmpIndex < lineStr.Length; ++tmpIndex)
							{
								if ((lineStr[tmpIndex] != ' ') && (lineStr[tmpIndex] != '\t'))
									break;
							}
							if (tmpIndex == lineStr.Length)
								tmpIndex = 0;
							if (tmpIndex != index)
								changed = true;
							SetPos1(selection, 0, tmpIndex, indexRel: false);
						}
						if (!changed)
						{
							foreach (var selection in ranges[RangeType.Selection])
								SetPos1(selection, 0, 0, indexRel: false);
							xScrollValue = 0;
						}
					}
					break;
				case Key.End:
					foreach (var selection in ranges[RangeType.Selection])
						if (controlDown)
							SetPos1(selection, Data.NumLines - 1, Int32.MaxValue, false, false);
						else
							SetPos1(selection, 0, Int32.MaxValue, indexRel: false);
					break;
				case Key.PageUp:
					foreach (var selection in ranges[RangeType.Selection])
						if (controlDown)
							yScrollValue -= numLines / 2;
						else
							SetPos1(selection, 1 - numLines, 0);
					break;
				case Key.PageDown:
					foreach (var selection in ranges[RangeType.Selection])
						if (controlDown)
							yScrollValue += numLines / 2;
						else
							SetPos1(selection, numLines - 1, 0);
					break;
				case Key.Tab:
					{
						if (!ranges[RangeType.Selection].Any(range => range.HasSelection()))
						{
							AddText("\t");
							break;
						}

						var lines = ranges[RangeType.Selection].Where(a => a.HasSelection()).ToDictionary(a => Data.GetOffsetLine(a.Start), a => Data.GetOffsetLine(a.End - 1));
						lines = lines.SelectMany(selection => Enumerable.Range(selection.Key, selection.Value - selection.Key + 1)).Distinct().OrderBy(lineNum => lineNum).ToDictionary(line => line, line => Data.GetOffset(line, 0));
						int offset;
						string replace;
						if (shiftDown)
						{
							offset = 1;
							replace = "";
							lines = lines.Where(entry => Data[entry.Key].StartsWith("\t")).ToDictionary(entry => entry.Key, entry => entry.Value);
						}
						else
						{
							offset = 0;
							replace = "\t";
							lines = lines.Where(entry => !String.IsNullOrWhiteSpace(Data[entry.Key])).ToDictionary(entry => entry.Key, entry => entry.Value);
						}

						var sels = lines.Select(line => new Range { Pos1 = line.Value + offset, Pos2 = line.Value }).ToList();
						var insert = sels.Select(range => replace).ToList();
						Replace(sels, insert, false);
					}
					break;
				case Key.Enter:
					AddText(Data.DefaultEnding);
					break;
				case Key.OemCloseBrackets:
					if (controlDown)
					{
						foreach (var selection in ranges[RangeType.Selection])
						{
							var newPos = Data.GetOppositeBracket(selection.Pos1);
							if (newPos != -1)
							{
								var line = Data.GetOffsetLine(newPos);
								var index = Data.GetOffsetIndex(newPos, line);
								SetPos1(selection, line, index, false, false);
							}
						}
						InvalidateVisual();
					}
					else
						e.Handled = false;
					break;
				case Key.System:
					switch (e.SystemKey)
					{
						case Key.Up:
						case Key.Down:
						case Key.Left:
						case Key.Right:
							var lineMult = 0;
							var indexMult = 0;
							switch (e.SystemKey)
							{
								case Key.Up: lineMult = -1; break;
								case Key.Down: lineMult = 1; break;
								case Key.Left: indexMult = -1; break;
								case Key.Right: indexMult = 1; break;
							}

							if (altOnly)
							{
								for (var offset = 0; offset < ranges[RangeType.Selection].Count; ++offset)
									SetPos1(ranges[RangeType.Selection][offset], offset * lineMult, offset * indexMult);
							}
							break;
						default: e.Handled = false; break;
					}
					break;
				default: e.Handled = false; break;
			}
		}

		enum WordSkipType
		{
			None,
			Char,
			Symbol,
			Space,
		}

		void MoveNextWord(Range selection)
		{
			WordSkipType moveType = WordSkipType.None;

			var line = Data.GetOffsetLine(selection.Pos1);
			var index = Data.GetOffsetIndex(selection.Pos1, line) - 1;
			string lineStr = null;
			int lineIndex = -1;
			while (true)
			{
				if (lineIndex != line)
				{
					lineStr = Data[line];
					lineIndex = line;
				}

				if (index >= lineStr.Length)
				{
					++line;
					if (line >= Data.NumLines)
					{
						SetPos1(selection, Data.NumLines - 1, Int32.MaxValue, false, false);
						return;
					}
					index = -1;
					continue;
				}

				++index;
				WordSkipType current;
				if ((index >= lineStr.Length) || (lineStr[index] == ' ') || (lineStr[index] == '\t'))
					current = WordSkipType.Space;
				else if ((Char.IsLetterOrDigit(lineStr[index])) || (lineStr[index] == '_'))
					current = WordSkipType.Char;
				else
					current = WordSkipType.Symbol;

				if (moveType == WordSkipType.None)
					moveType = current;

				if (current != moveType)
				{
					SetPos1(selection, line, index, false, false);
					return;
				}
			}
		}

		void MovePrevWord(Range selection)
		{
			WordSkipType moveType = WordSkipType.None;

			var line = Data.GetOffsetLine(selection.Pos1);
			var index = Data.GetOffsetIndex(selection.Pos1, line);
			int lastLine = -1, lastIndex = -1;
			string lineStr = null;
			int lineIndex = -1;
			while (true)
			{
				if (lineIndex != line)
				{
					lineStr = Data[line];
					lineIndex = line;
					if (index < 0)
						index = lineStr.Length;
				}

				if (index < 0)
				{
					--line;
					if (line < 0)
					{
						SetPos1(selection, 0, 0, false, false);
						return;
					}
					continue;
				}

				lastLine = line;
				lastIndex = index;

				--index;
				WordSkipType current;
				if ((index < 0) || (lineStr[index] == ' ') || (lineStr[index] == '\t'))
					current = WordSkipType.Space;
				else if ((Char.IsLetterOrDigit(lineStr[index])) || (lineStr[index] == '_'))
					current = WordSkipType.Char;
				else
					current = WordSkipType.Symbol;

				if (moveType == WordSkipType.None)
					moveType = current;

				if (current != moveType)
				{
					SetPos1(selection, lastLine, lastIndex, false, false);
					return;
				}
			}
		}

		void SetPos1(Range selection, int line, int index, bool lineRel = true, bool indexRel = true)
		{
			if ((lineRel) || (indexRel))
			{
				var startLine = Data.GetOffsetLine(selection.Pos1);
				var startIndex = Data.GetOffsetIndex(selection.Pos1, startLine);

				if (lineRel)
					line += startLine;
				if (indexRel)
					index += startIndex;
			}

			line = Math.Max(0, Math.Min(line, Data.NumLines - 1));
			index = Math.Max(0, Math.Min(index, Data[line].Length));
			selection.Pos1 = Data.GetOffset(line, index);

			if (!selecting)
				SetPos2(selection, line, index, false, false);

			InvalidateVisual();
		}

		void SetPos2(Range selection, int line, int index, bool lineRel = false, bool indexRel = false)
		{
			if ((lineRel) || (indexRel))
			{
				var startLine = Data.GetOffsetLine(selection.Pos2);
				var startIndex = Data.GetOffsetIndex(selection.Pos2, startLine);

				if (lineRel)
					line += startLine;
				if (indexRel)
					index += startIndex;
			}

			line = Math.Max(0, Math.Min(line, Data.NumLines - 1));
			index = Math.Max(0, Math.Min(index, Data[line].Length));
			selection.Pos2 = Data.GetOffset(line, index);
			InvalidateVisual();
		}

		void MouseHandler(Point mousePos)
		{
			var line = Math.Min(Data.NumLines - 1, (int)(mousePos.Y / lineHeight) + yScrollValue);
			var index = GetIndexFromColumn(line, (int)(mousePos.X / charWidth) + xScrollValue);

			Range selection;
			if (selecting)
				selection = ranges[RangeType.Selection].Last();
			else
			{
				if (!controlDown)
					ranges[RangeType.Selection].Clear();

				selection = new Range();
				ranges[RangeType.Selection].Add(selection);
			}
			SetPos1(selection, line, index, false, false);
		}

		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			MouseHandler(e.GetPosition(this));
			mouseDown = e.ButtonState == MouseButtonState.Pressed;
			if (mouseDown)
				CaptureMouse();
			else
				ReleaseMouseCapture();
			e.Handled = true;
		}

		protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
		{
			OnMouseLeftButtonDown(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (!mouseDown)
				return;

			MouseHandler(e.GetPosition(this));
			e.Handled = true;
		}

		void RunSearch(Regex regex, bool selectionOnly)
		{
			if (regex == null)
				return;

			ranges[RangeType.Search].Clear();

			for (var line = 0; line < Data.NumLines; line++)
			{
				var lineStr = Data[line];
				var matches = regex.Matches(lineStr);
				foreach (Match match in matches)
				{
					var searchResult = new Range { Pos1 = Data.GetOffset(line, match.Index + match.Length), Pos2 = Data.GetOffset(line, match.Index) };

					if (selectionOnly)
					{
						var foundMatch = false;
						foreach (var selection in ranges[RangeType.Selection])
						{
							if ((searchResult.Start < selection.Start) || (searchResult.End > selection.End))
								continue;

							foundMatch = true;
							break;
						}
						if (!foundMatch)
							continue;
					}

					ranges[RangeType.Search].Add(searchResult);
				}
			}
			InvalidateVisual();
		}

		string GetString(Range range)
		{
			return Data.GetString(range.Start, range.End);
		}

		enum ReplaceType
		{
			Normal,
			Undo,
			Redo,
		}

		const int maxUndoChars = 1048576 * 10;
		void AddUndoRedo(TextCanvasUndoRedo current, ReplaceType replaceType)
		{
			switch (replaceType)
			{
				case ReplaceType.Undo:
					redo.Add(current);
					break;
				case ReplaceType.Redo:
					undo.Add(current);
					break;
				case ReplaceType.Normal:
					redo.Clear();

					// See if we can add this one to the last one
					bool done = false;
					if (undo.Count != 0)
					{
						var last = undo.Last();
						if (last.ranges.Count == current.ranges.Count)
						{
							var change = 0;
							done = true;
							for (var num = 0; num < last.ranges.Count; ++num)
							{
								if (last.ranges[num].End + change != current.ranges[num].Start)
								{
									done = false;
									break;
								}
								change += current.ranges[num].Length - current.text[num].Length;
							}

							if (done)
							{
								change = 0;
								for (var num = 0; num < last.ranges.Count; ++num)
								{
									last.ranges[num] = new Range { Pos1 = last.ranges[num].Start + change, Pos2 = last.ranges[num].End + current.ranges[num].Length + change };
									last.text[num] += current.text[num];
									change += current.ranges[num].Length - current.text[num].Length;
								}
							}
						}
					}

					if (!done)
						undo.Add(current);

					// Limit undo buffer
					while (true)
					{
						var totalChars = undo.Sum(undoItem => undoItem.text.Sum(textItem => textItem.Length));
						if (totalChars <= maxUndoChars)
							break;
						undo.RemoveAt(0);
					}
					break;
			}
		}

		void Replace(List<Range> replaceRanges, List<string> strs, bool leaveHighlighted, ReplaceType replaceType = ReplaceType.Normal)
		{
			if (strs == null)
				strs = replaceRanges.Select(range => "").ToList();

			var undoRanges = new List<Range>();
			var undoText = new List<string>();

			var change = 0;
			for (var ctr = 0; ctr < replaceRanges.Count; ++ctr)
			{
				var undoRange = new Range { Pos1 = replaceRanges[ctr].Start + change, Pos2 = replaceRanges[ctr].Start + strs[ctr].Length + change };
				undoRanges.Add(undoRange);
				undoText.Add(GetString(replaceRanges[ctr]));
				change = undoRange.Pos2 - replaceRanges[ctr].End;
			}

			AddUndoRedo(new TextCanvasUndoRedo(undoRanges, undoText), replaceType);

			Data.Replace(replaceRanges.Select(range => range.Start).ToList(), replaceRanges.Select(range => range.Length).ToList(), strs);

			ranges[RangeType.Search].Clear();

			var numsToMap = ranges.SelectMany(rangePair => rangePair.Value).SelectMany(range => new int[] { range.Start, range.End }).Distinct().OrderBy(num => num).ToList();
			var oldToNewMap = new Dictionary<int, int>();
			var replaceRange = 0;
			var offset = 0;
			while (numsToMap.Count != 0)
			{
				int start = Int32.MaxValue, end = Int32.MaxValue, length = 0;
				if (replaceRange < replaceRanges.Count)
				{
					start = replaceRanges[replaceRange].Start;
					end = replaceRanges[replaceRange].End;
					length = strs[replaceRange].Length;
				}

				if (numsToMap[0] >= end)
				{
					offset += start - end + length;
					++replaceRange;
					continue;
				}

				var value = numsToMap[0];
				if ((value > start) && (value < end))
					value = start + length;

				oldToNewMap[numsToMap[0]] = value + offset;
				numsToMap.RemoveAt(0);
			}

			foreach (var range in ranges.SelectMany(rangePair => rangePair.Value))
			{
				range.Pos1 = oldToNewMap[range.Pos1];
				range.Pos2 = oldToNewMap[range.Pos2];
			}

			if (!leaveHighlighted)
				replaceRanges.ForEach(range => range.Pos1 = range.Pos2 = range.End);

			InvalidateVisual();
		}

		void FindNext(bool forward)
		{
			if (ranges[RangeType.Search].Count == 0)
				return;

			foreach (var selection in ranges[RangeType.Selection])
			{
				int index;
				if (forward)
				{
					index = ranges[RangeType.Search].FindIndex(range => range.Start > selection.Start);
					if (index == -1)
						index = 0;
				}
				else
				{
					index = ranges[RangeType.Search].FindLastIndex(range => range.Start < selection.Start);
					if (index == -1)
						index = ranges[RangeType.Search].Count - 1;
				}

				selection.Pos1 = ranges[RangeType.Search][index].End;
				selection.Pos2 = ranges[RangeType.Search][index].Start;
			}
			InvalidateVisual();
		}

		string SetWidth(string str, int length, char padChar, bool before)
		{
			var pad = new string(padChar, length - str.Length);
			if (before)
				return pad + str;
			return str + pad;
		}

		string SortStr(string str)
		{
			return Regex.Replace(str, @"\d+", match => new string('0', Math.Max(0, 20 - match.Value.Length)) + match.Value);
		}

		public void RunCommand(ICommand command)
		{
			InvalidateVisual();

			if (command == TextEditorWindow.Command_Edit_Undo)
			{
				if (undo.Count == 0)
					return;

				var undoStep = undo.Last();
				undo.Remove(undoStep);
				Replace(undoStep.ranges, undoStep.text, true, ReplaceType.Undo);
			}
			else if (command == TextEditorWindow.Command_Edit_Redo)
			{
				if (redo.Count == 0)
					return;

				var redoStep = redo.Last();
				redo.Remove(redoStep);
				Replace(redoStep.ranges, redoStep.text, true, ReplaceType.Redo);
			}
			else if ((command == TextEditorWindow.Command_Edit_Cut) || (command == TextEditorWindow.Command_Edit_Copy))
			{
				var result = ranges[RangeType.Selection].Select(range => GetString(range)).ToArray();
				if (result.Length != 0)
					ClipboardWindow.Set(result);
				if (command == TextEditorWindow.Command_Edit_Cut)
					Replace(ranges[RangeType.Selection], null, false);
			}
			else if (command == TextEditorWindow.Command_Edit_Paste)
			{
				var result = ClipboardWindow.GetStrings().ToList();
				if ((result == null) || (result.Count == 0))
					return;

				var sels = ranges[RangeType.Selection];
				var separator = sels.Count == 1 ? Data.DefaultEnding : " ";
				while (result.Count > sels.Count)
				{
					result[result.Count - 2] += separator + result[result.Count - 1];
					result.RemoveAt(result.Count - 1);
				}
				while (result.Count < sels.Count)
					result.Add(result.Last());

				Replace(sels, result, false);
			}
			else if (command == TextEditorWindow.Command_Edit_Find)
			{
				var selectionOnly = ranges[RangeType.Selection].Any(range => range.HasSelection());
				var findDialog = new FindDialog { SelectionOnly = selectionOnly };
				if (findDialog.ShowDialog() != true)
					return;

				RunSearch(findDialog.Regex, findDialog.SelectionOnly);
				if (findDialog.SelectAll)
				{
					if (ranges[RangeType.Search].Count != 0)
						ranges[RangeType.Selection] = ranges[RangeType.Search];
					ranges[RangeType.Search] = new List<Range>();
				}

				FindNext(true);
			}
			else if ((command == TextEditorWindow.Command_Edit_FindNext) || (command == TextEditorWindow.Command_Edit_FindPrev))
				FindNext(command == TextEditorWindow.Command_Edit_FindNext);
			else if (command == TextEditorWindow.Command_Edit_GotoLine)
			{
				var shift = shiftDown;
				var line = Data.GetOffsetLine(ranges[RangeType.Selection].First().Start);
				var getNumDialog = new GetNumDialog
				{
					Title = "Go to line",
					Text = String.Format("Go to line: (1 - {0})", Data.NumLines),
					MinValue = 1,
					MaxValue = Data.NumLines,
					Value = line,
				};
				if (getNumDialog.ShowDialog() == true)
				{
					shiftOverride = shift;
					foreach (var selection in ranges[RangeType.Selection])
						SetPos1(selection, (int)getNumDialog.Value - 1, 0, false, true);
					shiftOverride = null;
				}
			}
			else if (command == TextEditorWindow.Command_Edit_GotoIndex)
			{
				var offset = ranges[RangeType.Selection].First().Start;
				var line = Data.GetOffsetLine(offset);
				var index = Data.GetOffsetIndex(offset, line);
				var getNumDialog = new GetNumDialog
				{
					Title = "Go to column",
					Text = String.Format("Go to column: (1 - {0})", Data[line].Length + 1),
					MinValue = 1,
					MaxValue = Data[line].Length + 1,
					Value = index,
				};
				if (getNumDialog.ShowDialog() == true)
				{
					foreach (var selection in ranges[RangeType.Selection])
						SetPos1(selection, 0, (int)getNumDialog.Value - 1, true, false);
				}
			}
			else if (command == TextEditorWindow.Command_Edit_BOM)
			{
				if (Data.BOM)
					Replace(new List<Range> { new Range { Pos1 = 0, Pos2 = 1 } }, new List<string> { "" }, true);
				else
					Replace(new List<Range> { new Range { Pos1 = 0, Pos2 = 0 } }, new List<string> { "\ufeff" }, true);
			}
			else if (command == TextEditorWindow.Command_Data_ToUpper)
			{
				var selections = ranges[RangeType.Selection];
				var strs = selections.Select(range => GetString(range).ToUpperInvariant()).ToList();
				Replace(selections, strs, true);
			}
			else if (command == TextEditorWindow.Command_Data_ToLower)
			{
				var selections = ranges[RangeType.Selection];
				var strs = selections.Select(range => GetString(range).ToLowerInvariant()).ToList();
				Replace(selections, strs, true);
			}
			else if (command == TextEditorWindow.Command_Data_ToHex)
			{
				var selections = ranges[RangeType.Selection].Where(range => range.HasSelection()).ToList();
				var strs = selections.Select(range => Int64.Parse(GetString(range)).ToString("x")).ToList();
				Replace(selections, strs, true);
			}
			else if (command == TextEditorWindow.Command_Data_FromHex)
			{
				var selections = ranges[RangeType.Selection].Where(range => range.HasSelection()).ToList();
				var strs = selections.Select(range => Int64.Parse(GetString(range), NumberStyles.HexNumber).ToString()).ToList();
				Replace(selections, strs, true);
			}
			else if (command == TextEditorWindow.Command_Data_ToChar)
			{
				var selections = ranges[RangeType.Selection].Where(range => range.HasSelection()).ToList();
				var strs = selections.Select(range => ((char)UInt16.Parse(GetString(range), NumberStyles.HexNumber)).ToString()).ToList();
				Replace(selections, strs, true);
			}
			else if (command == TextEditorWindow.Command_Data_FromChar)
			{
				var selections = ranges[RangeType.Selection].Where(range => range.Length == 1).ToList();
				var strs = selections.Select(range => ((UInt16)GetString(range)[0]).ToString("x2")).ToList();
				Replace(selections, strs, true);
			}
			else if (command == TextEditorWindow.Command_Data_Length)
			{
				var selections = ranges[RangeType.Selection].Where(range => range.HasSelection()).ToList();
				var strs = selections.Select(range => GetString(range).Length.ToString()).ToList();
				Replace(selections, strs, true);
			}
			else if (command == TextEditorWindow.Command_Data_Width)
			{
				var selections = ranges[RangeType.Selection];
				var minWidth = selections.Select(range => range.Length).Max();
				var text = String.Join("", selections.Select(range => GetString(range)));
				var numeric = Regex.IsMatch(text, "^[0-9a-fA-F]+$");
				var widthDialog = new WidthDialog { MinWidthNum = minWidth, PadChar = numeric ? '0' : ' ', Before = numeric };
				if (widthDialog.ShowDialog() == true)
					Replace(selections, selections.Select(range => SetWidth(GetString(range), widthDialog.WidthNum, widthDialog.PadChar, widthDialog.Before)).ToList(), true);
			}
			else if (command == TextEditorWindow.Command_Data_Trim)
			{
				var selections = ranges[RangeType.Selection].Where(range => range.HasSelection()).ToList();
				var strs = selections.Select(range => GetString(range).Trim().TrimStart('0')).ToList();
				Replace(selections, strs, true);
			}
			else if (command == TextEditorWindow.Command_Data_SetKeys)
			{
				var keys = ranges[RangeType.Selection].Select(range => GetString(range)).ToList();
				if (keys.Distinct().Count() != keys.Count)
					throw new ArgumentException("Cannot have duplicate keys.");
				keysToValues = keys.ToDictionary(key => key, key => key);
			}
			else if (command == TextEditorWindow.Command_Data_SetValues)
			{
				var keys = keysToValues.Keys.ToList();
				var values = ranges[RangeType.Selection].Select(range => GetString(range)).ToList();
				if (values.Count() != keys.Count)
					throw new ArgumentException("Key count must match value count.");
				keysToValues = Enumerable.Range(0, keys.Count).ToDictionary(num => keys[num], num => values[num]);
			}
			else if (command == TextEditorWindow.Command_Data_KeysToValues)
			{
				var selections = ranges[RangeType.Selection];
				var strs = selections.Select(range => GetString(range)).Select(sel => keysToValues.ContainsKey(sel) ? keysToValues[sel] : sel).ToList();
				Replace(selections, strs, true);
			}
			else if (command == TextEditorWindow.Command_Data_Reverse)
			{
				var selections = ranges[RangeType.Selection];
				var strs = selections.Select(range => GetString(range)).Reverse().ToList();
				Replace(selections, strs, true);
			}
			else if (command == TextEditorWindow.Command_Data_Sort)
			{
				var selections = ranges[RangeType.Selection];
				var strs = selections.Select(range => GetString(range)).OrderBy(str => SortStr(str)).ToList();
				Replace(selections, strs, true);
			}
			else if (command == TextEditorWindow.Command_Data_Evaluate)
			{
				var selections = ranges[RangeType.Selection].Where(range => range.HasSelection()).ToList();
				var strs = selections.Select(range => GetString(range)).Select(expr => new NeoEdit.Common.Expression(expr).Evaluate().ToString()).ToList();
				Replace(selections, strs, true);
			}
			else if (command == TextEditorWindow.Command_Data_Unique)
			{
				var selections = ranges[RangeType.Selection];
				var dups = selections.GroupBy(range => GetString(range)).Select(list => list.First()).ToList();
				ranges[RangeType.Selection] = dups;
			}
			else if (command == TextEditorWindow.Command_Data_Duplicates)
			{
				var selections = ranges[RangeType.Selection];
				var dups = selections.GroupBy(range => GetString(range)).SelectMany(list => list.Skip(1)).ToList();
				if (dups.Count != 0)
					ranges[RangeType.Selection] = dups;
			}
			else if (command == TextEditorWindow.Command_Data_Randomize)
			{
				var rng = new Random();
				var strs = ranges[RangeType.Selection].Select(range => GetString(range)).OrderBy(range => rng.Next()).ToList();
				Replace(ranges[RangeType.Selection], strs, true);
			}
			else if (command == TextEditorWindow.Command_Data_Series)
			{
				var strs = Enumerable.Range(1, ranges[RangeType.Selection].Count).Select(num => num.ToString()).ToList();
				Replace(ranges[RangeType.Selection], strs, true);
			}
			else if (command == TextEditorWindow.Command_Data_SortLineBySelection)
			{
				if (ranges[RangeType.Selection].Any(selection => Data.GetOffsetLine(selection.Start) != Data.GetOffsetLine(selection.End - 1)))
					throw new Exception("Selections must stay on the same line");
				if (ranges[RangeType.Selection].Select(selection => Data.GetOffsetLine(selection.Start)).GroupBy(line => line).Any(group => group.Count() != 1))
					throw new Exception("Only one selections per line");

				var sortStrAndLine = ranges[RangeType.Selection].Select(selection => new { sortStr = GetString(selection), line = Data.GetOffsetLine(selection.Start) }).ToList();
				var sortStrAndOffsetAndLen = sortStrAndLine.Select(data => new { sortStr = data.sortStr, offset = Data.GetOffset(data.line, 0), length = Data[data.line].Length }).ToList();
				var sortStrAndRange = sortStrAndOffsetAndLen.Select(data => new { sortStr = data.sortStr, range = new Range { Pos1 = data.offset, Pos2 = data.offset + data.length } }).ToList();
				var sortStrAndRangeAndLine = sortStrAndRange.Select(data => new { sortStr = data.sortStr, range = data.range, line = GetString(data.range) }).ToList();

				var lineRanges = sortStrAndRangeAndLine.Select(data => data.range).ToList();
				var replaceStrs = sortStrAndRangeAndLine.OrderBy(data => data.sortStr).Select(data => data.line).ToList();

				Replace(lineRanges, replaceStrs, true);
			}
			else if (command == TextEditorWindow.Command_Data_SortByLength)
			{
				var selections = ranges[RangeType.Selection];
				var strs = selections.Select(range => GetString(range)).OrderBy(str => str.Length).ToList();
				Replace(selections, strs, true);
			}
			else if (command == TextEditorWindow.Command_Data_MD5)
			{
				var strs = ranges[RangeType.Selection].Select(range => Checksum.Get(Checksum.Type.MD5, Encoding.UTF8.GetBytes(GetString(range)))).ToList();
				Replace(ranges[RangeType.Selection], strs, true);
			}
			else if (command == TextEditorWindow.Command_Data_SHA1)
			{
				var strs = ranges[RangeType.Selection].Select(range => Checksum.Get(Checksum.Type.SHA1, Encoding.UTF8.GetBytes(GetString(range)))).ToList();
				Replace(ranges[RangeType.Selection], strs, true);
			}
			else if (command == TextEditorWindow.Command_SelectMark_Toggle)
			{
				if (ranges[RangeType.Selection].Count > 1)
				{
					ranges[RangeType.Mark].AddRange(ranges[RangeType.Selection].Select(range => range.Copy()));
					ranges[RangeType.Selection] = new List<Range> { ranges[RangeType.Selection].First() };
				}
				else if (ranges[RangeType.Mark].Count != 0)
				{
					ranges[RangeType.Selection] = ranges[RangeType.Mark];
					ranges[RangeType.Mark] = new List<Range>();
				}
			}
			else if (command == TextEditorWindow.Command_Select_All)
			{
				foreach (var selection in ranges[RangeType.Selection])
				{
					SetPos1(selection, Int32.MaxValue, Int32.MaxValue, false, false);
					SetPos2(selection, 0, 0, false, false);
				}
			}
			else if (command == TextEditorWindow.Command_Select_Unselect)
			{
				ranges[RangeType.Selection].ForEach(range => range.Pos1 = range.Pos2 = range.Start);
				InvalidateVisual();
			}
			else if (command == TextEditorWindow.Command_Select_Single)
				ranges[RangeType.Selection] = new List<Range> { ranges[RangeType.Selection].First() };
			else if (command == TextEditorWindow.Command_Select_Lines)
			{
				var selectLinesDialog = new SelectLinesDialog();
				if (selectLinesDialog.ShowDialog() != true)
					return;

				var lines = ranges[RangeType.Selection].SelectMany(selection => Enumerable.Range(Data.GetOffsetLine(selection.Start), Data.GetOffsetLine(selection.End - 1) - Data.GetOffsetLine(selection.Start) + 1)).Distinct().OrderBy(lineNum => lineNum).ToList();
				var sels = lines.Select(line => new Range { Pos1 = Data.GetOffset(line, Data[line].Length), Pos2 = Data.GetOffset(line, 0) }).ToList();
				if (selectLinesDialog.IgnoreBlankLines)
					sels = sels.Where(sel => sel.Pos1 != sel.Pos2).ToList();
				if (selectLinesDialog.LineMult > 1)
					sels = sels.Where((sel, index) => index % selectLinesDialog.LineMult == 0).ToList();
				ranges[RangeType.Selection] = sels;
				InvalidateVisual();
			}
			else if (command == TextEditorWindow.Command_Select_Find)
			{
				ranges[RangeType.Selection] = ranges[RangeType.Search];
				ranges[RangeType.Search] = new List<Range>();
			}
			else if (command == TextEditorWindow.Command_Select_RemoveEmpty)
				ranges[RangeType.Selection] = ranges[RangeType.Selection].Where(range => range.HasSelection()).ToList();
			else if (command == TextEditorWindow.Command_Select_Marks)
			{
				if (ranges[RangeType.Mark].Count == 0)
					return;

				ranges[RangeType.Selection] = ranges[RangeType.Mark];
				ranges[RangeType.Mark] = new List<Range>();
			}
			else if (command == TextEditorWindow.Command_Mark_Find)
			{
				ranges[RangeType.Mark].AddRange(ranges[RangeType.Search]);
				ranges[RangeType.Search] = new List<Range>();
			}
			else if (command == TextEditorWindow.Command_Mark_Selection)
			{
				ranges[RangeType.Mark].AddRange(ranges[RangeType.Selection].Select(range => range.Copy()));
			}
			else if (command == TextEditorWindow.Command_Mark_LimitToSelection)
			{
				ranges[RangeType.Mark] = ranges[RangeType.Mark].Where(mark => ranges[RangeType.Selection].Any(selection => (mark.Start >= selection.Start) && (mark.End <= selection.End))).ToList();
			}
			else if (command == TextEditorWindow.Command_Mark_Clear)
			{
				var hasSelection = ranges[RangeType.Selection].Any(range => range.HasSelection());
				if (!hasSelection)
					ranges[RangeType.Mark].Clear();
				else
				{
					foreach (var selection in ranges[RangeType.Selection])
					{
						var toRemove = ranges[RangeType.Mark].Where(mark => (mark.Start >= selection.Start) && (mark.End <= selection.End)).ToList();
						toRemove.ForEach(mark => ranges[RangeType.Mark].Remove(mark));
					}
				}
			}
		}

		void AddText(string text)
		{
			if (text.Length == 0)
				return;

			Replace(ranges[RangeType.Selection], ranges[RangeType.Selection].Select(range => text).ToList(), false);
		}

		protected override void OnTextInput(TextCompositionEventArgs e)
		{
			base.OnTextInput(e);
			if (e.Handled)
				return;

			AddText(e.Text);
			e.Handled = true;
		}
	}
}
