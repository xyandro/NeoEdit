using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NeoEdit.Common;

namespace NeoEdit.TextEditorUI
{
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

		public bool HasSelection()
		{
			return Pos1 != Pos2;
		}

		public override string ToString()
		{
			return String.Format("({0:0000000000})->({1:0000000000})", Start, End);
		}
	};

	public partial class TextCanvas : Canvas
	{
		[DepProp]
		public TextData Data { get { return uiHelper.GetPropValue<TextData>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public long ChangeCount { get { return uiHelper.GetPropValue<long>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public Highlighting.HighlightingType HighlightType { get { return uiHelper.GetPropValue<Highlighting.HighlightingType>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public double xScrollMaximum { get { return uiHelper.GetPropValue<double>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public double xScrollSmallChange { get { return uiHelper.GetPropValue<double>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public double xScrollLargeChange { get { return uiHelper.GetPropValue<double>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public double xScrollValue { get { return uiHelper.GetPropValue<double>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public double yScrollMaximum { get { return uiHelper.GetPropValue<double>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public double yScrollSmallChange { get { return uiHelper.GetPropValue<double>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public double yScrollLargeChange { get { return uiHelper.GetPropValue<double>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public double yScrollValue { get { return uiHelper.GetPropValue<double>(); } set { uiHelper.SetPropValue(value); } }

		int lines, columns;

		// X spacing
		const int tabStop = 4;
		const double xStartSpacing = 10;
		readonly double charWidth;
		const double xEndSpacing = xStartSpacing;

		double xStart { get { return 0; } }
		double xLineStart { get { return xStart + xStartSpacing; } }
		double xLineEnd { get { return xLineStart + columns * charWidth; } }
		double xEnd { get { return xLineEnd + xEndSpacing; } }

		// Y spacing
		const double yStartSpacing = 10;
		readonly double lineHeight;
		const double yEndSpacing = yStartSpacing;

		double yStart { get { return 0; } }
		double yLinesStart { get { return yStart + yStartSpacing; } }
		double yLinesEnd { get { return yLinesStart + lines * lineHeight; } }
		double yEnd { get { return yLinesEnd + yEndSpacing; } }

		enum RangeType
		{
			Search,
			Mark,
			Selection,
		}
		Dictionary<RangeType, List<Range>> ranges = Helpers.GetValues<RangeType>().ToDictionary(rangeType => rangeType, rangeType => new List<Range>());

		readonly Typeface typeface;
		readonly double fontSize;

		static TextCanvas() { UIHelper<TextCanvas>.Register(); }

		readonly UIHelper<TextCanvas> uiHelper;
		public TextCanvas()
		{
			uiHelper = new UIHelper<TextCanvas>(this);
			InitializeComponent();

			var fontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./Resources/#Anonymous Pro");
			typeface = fontFamily.GetTypefaces().First();
			fontSize = 14;
			lineHeight = fontSize;

			var example = "0123456789 abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ !@#$%^&*()";
			var formattedText = new FormattedText(example, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
			charWidth = formattedText.Width / example.Length;

			uiHelper.AddCallback(a => a.Data, (o, n) => InvalidateVisual());
			uiHelper.AddCallback(a => a.ChangeCount, (o, n) => { ranges[RangeType.Search].Clear(); InvalidateVisual(); });
			uiHelper.AddCallback(a => a.xScrollValue, (o, n) => InvalidateVisual());
			uiHelper.AddCallback(a => a.yScrollValue, (o, n) => InvalidateVisual());
			uiHelper.AddCallback(a => a.HighlightType, (o, n) => InvalidateVisual());
			uiHelper.AddCallback(Canvas.ActualWidthProperty, this, () => InvalidateVisual());
			uiHelper.AddCallback(Canvas.ActualHeightProperty, this, () => { EnsureVisible(ranges[RangeType.Selection].First()); InvalidateVisual(); });

			Loaded += (s, e) => InvalidateVisual();
		}

		internal void EnsureVisible(Range selection)
		{
			var line = Data.GetOffsetLine(selection.Pos1);
			var index = Data.GetOffsetIndex(selection.Pos1, line);
			var y = GetYFromLine(line);
			yScrollValue = Math.Min(y, Math.Max(y + lineHeight - ActualHeight, yScrollValue));
			var x = GetXFromLineIndex(line, index);
			xScrollValue = Math.Min(x, Math.Max(x + charWidth - ActualWidth, xScrollValue));
		}

		double GetXFromLineIndex(int line, int index)
		{
			var column = GetColumnFromIndex(line, index);
			return xLineStart + column * charWidth;
		}

		int GetColumnFromX(double x)
		{
			return (int)((x - xLineStart) / charWidth);
		}

		double GetXFromColumn(int column)
		{
			return xLineStart + column * charWidth;
		}

		int GetLineFromY(double y)
		{
			return (int)((y - yLinesStart) / lineHeight);
		}

		double GetYFromLine(int line)
		{
			return yLinesStart + line * lineHeight;
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

		int GetIndexFromColumn(int line, int index)
		{
			return GetIndexFromColumn(Data[line], index);
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

			columns = Enumerable.Range(0, Data.NumLines).Select(lineNum => Data[lineNum]).Select(line => GetColumnFromIndex(line, line.Length)).Max();
			lines = Data.NumLines;

			xScrollMaximum = xEnd - ActualWidth;
			xScrollSmallChange = charWidth;
			xScrollLargeChange = ActualWidth - xScrollSmallChange;

			yScrollMaximum = yEnd - ActualHeight;
			yScrollSmallChange = lineHeight;
			yScrollLargeChange = ActualHeight - yScrollSmallChange;

			ranges[RangeType.Mark] = ranges[RangeType.Mark].Where(range => range.HasSelection()).OrderBy(range => range.Start).ToList();
			ranges[RangeType.Search] = ranges[RangeType.Search].Where(range => range.HasSelection()).OrderBy(range => range.Start).ToList();
			if (ranges[RangeType.Selection].Count == 0)
			{
				var range = new Range();
				ranges[RangeType.Selection].Add(range);
				SetPos1(range, 0, 0, false, false);
			}


			for (var ctr1 = 0; ctr1 < ranges[RangeType.Mark].Count; ++ctr1)
			{
				var mark1 = ranges[RangeType.Mark][ctr1];
				var ctr2 = ctr1 + 1;
				while (ctr2 < ranges[RangeType.Mark].Count)
				{
					var mark2 = ranges[RangeType.Mark][ctr2];
					if ((mark1.Start < mark2.End) && (mark1.End > mark2.Start))
					{
						mark1.Pos1 = Math.Min(mark1.Start, mark2.Start);
						mark1.Pos2 = Math.Max(mark1.End, mark2.End);
						ranges[RangeType.Mark].RemoveAt(ctr2);
					}
					else
						++ctr2;
				}
			}

			ranges[RangeType.Selection] = ranges[RangeType.Selection].GroupBy(range => range.ToString()).Select(rangeGroup => rangeGroup.First()).OrderBy(range => range.Start).ToList();
			// Make sure selections don't overlap
			for (var ctr = 0; ctr < ranges[RangeType.Selection].Count - 1; ctr++)
			{
				ranges[RangeType.Selection][ctr].Pos1 = Math.Min(ranges[RangeType.Selection][ctr].Pos1, ranges[RangeType.Selection][ctr + 1].Start);
				ranges[RangeType.Selection][ctr].Pos2 = Math.Min(ranges[RangeType.Selection][ctr].Pos2, ranges[RangeType.Selection][ctr + 1].Start);
			}

			var startLine = Math.Max(0, GetLineFromY(yScrollValue));
			var endLine = Math.Min(lines, GetLineFromY(ActualHeight + lineHeight + yScrollValue));

			var highlightDictionary = Highlighting.Get(HighlightType).GetDictionary();

			for (var line = startLine; line < endLine; ++line)
			{
				var lineStr = Data[line];
				var lineRange = new Range { Pos1 = Data.GetOffset(line, 0), Pos2 = Data.GetOffset(line, lineStr.Length) };
				var y = yLinesStart - yScrollValue + line * lineHeight;

				foreach (var entry in ranges)
				{
					foreach (var range in entry.Value)
					{
						if ((!range.HasSelection()) || (range.End < lineRange.Start) || (range.Start > lineRange.End))
							continue;

						var start = Math.Max(lineRange.Start, range.Start);
						var end = Math.Min(lineRange.End, range.End);
						start = Data.GetOffsetIndex(start, line);
						end = Data.GetOffsetIndex(end, line);
						start = GetColumnFromIndex(lineStr, start);
						end = GetColumnFromIndex(lineStr, end);
						if (range.End > lineRange.End)
							end++;

						dc.DrawRectangle(brushes[entry.Key], null, new Rect(GetXFromColumn(start) - xScrollValue, y, (end - start) * charWidth, lineHeight));
					}
				}

				foreach (var selection in ranges[RangeType.Selection])
				{
					if ((selection.Pos1 < lineRange.Start) || (selection.Pos1 > lineRange.End))
						continue;

					var column = GetColumnFromIndex(lineStr, Data.GetOffsetIndex(selection.Pos1, line));
					dc.DrawRectangle(Brushes.Black, null, new Rect(GetXFromColumn(column) - xScrollValue, y, 1, lineHeight));
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
				var text = new FormattedText(str, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
				foreach (var entry in highlightDictionary)
				{
					var matches = entry.Key.Matches(str);
					foreach (Match match in matches)
						text.SetForegroundBrush(entry.Value, match.Index, match.Length);
				}
				dc.DrawText(text, new Point(xLineStart - xScrollValue, y));
			}
		}

		bool mouseDown;
		bool shiftDown { get { return (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None; } }
		bool controlDown { get { return (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None; } }
		bool altOnly { get { return (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt)) == ModifierKeys.Alt; } }
		internal bool selecting { get { return (mouseDown) || (shiftDown); } }

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
						{
							yScrollValue += lineHeight * mult;
							if (ranges[RangeType.Selection].Count == 1)
							{
								var line = Data.GetOffsetLine(ranges[RangeType.Selection][0].Pos1);
								var adj = Math.Min(0, line - GetLineFromY(yScrollValue + lineHeight - 1)) + Math.Max(0, line - GetLineFromY(yScrollValue + ActualHeight - lineHeight));
								SetPos1(ranges[RangeType.Selection][0], -adj, 0);

							}
						}
						else
						{
							foreach (var selection in ranges[RangeType.Selection])
								SetPos1(selection, mult, 0);
						}
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
							SetPos1(selection, GetLineFromY(yScrollValue + lineHeight - 1), 0, lineRel: false);
						else
							SetPos1(selection, -(int)(ActualHeight / lineHeight - 1), 0);
					break;
				case Key.PageDown:
					foreach (var selection in ranges[RangeType.Selection])
						if (controlDown)
							SetPos1(selection, GetLineFromY(ActualHeight + yScrollValue - lineHeight), 0, lineRel: false);
						else
							SetPos1(selection, (int)(ActualHeight / lineHeight - 1), 0);
					break;
				case Key.A:
					if (controlDown)
					{
						foreach (var selection in ranges[RangeType.Selection])
						{
							SetPos1(selection, Int32.MaxValue, Int32.MaxValue, false, false);
							SetPos2(selection, 0, 0, false, false);
						}
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

			EnsureVisible(selection);
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
			var line = Math.Min(Data.NumLines - 1, GetLineFromY(mousePos.Y + yScrollValue));
			var index = GetIndexFromColumn(line, GetColumnFromX(mousePos.X + xScrollValue));

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
					var searchResult = new Range { Pos1 = Data.GetOffset(line, match.Index), Pos2 = Data.GetOffset(line, match.Index + match.Length) };

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

		void Replace(List<Range> replaceRanges, List<string> strs, bool leaveHighlighted)
		{
			if (strs == null)
				strs = replaceRanges.Select(range => "").ToList();

			Data.Replace(replaceRanges.Select(range => range.Start).ToList(), replaceRanges.Select(range => range.End - range.Start).ToList(), strs);

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

				EnsureVisible(selection);
			}
			InvalidateVisual();
		}

		public string SetWidth(string str, int length, char padChar, bool before)
		{
			var pad = new string(padChar, length - str.Length);
			if (before)
				return pad + str;
			return str + pad;
		}

		public void CommandRun(UICommand command, object parameter)
		{
			try
			{
				switch (command.Name)
				{
					case "Edit_Undo":
						ranges[RangeType.Mark].Clear();
						ranges[RangeType.Search].Clear();
						ranges[RangeType.Selection].Clear();
						Data.Undo();
						break;
					case "Edit_Redo":
						Data.Redo();
						break;
					case "Edit_Cut":
					case "Edit_Copy":
						{
							var result = ranges[RangeType.Selection].Where(range => range.HasSelection()).Select(range => GetString(range)).ToArray();
							if (result.Length != 0)
								Clipboard.Current.Set(result);
							if (command.Name == "Edit_Cut")
								Replace(ranges[RangeType.Selection], null, false);
						}
						break;
					case "Edit_Paste":
						{
							var result = Clipboard.Current.GetStrings().ToList();
							if ((result == null) || (result.Count == 0))
								break;

							var sels = ranges[RangeType.Selection];
							while (result.Count > sels.Count)
							{
								result[result.Count - 2] += " " + result[result.Count - 1];
								result.RemoveAt(result.Count - 1);
							}
							while (result.Count < sels.Count)
								result.Add(result.Last());

							Replace(sels, result, false);
						}
						break;
					case "Edit_Find":
						{
							var selectionOnly = ranges[RangeType.Selection].Any(range => range.HasSelection());
							var findDialog = new FindDialog { SelectionOnly = selectionOnly };
							if (findDialog.ShowDialog() != true)
								break;

							RunSearch(findDialog.Regex, findDialog.SelectionOnly);
							if (findDialog.SelectAll)
							{
								ranges[RangeType.Selection] = ranges[RangeType.Search];
								ranges[RangeType.Search] = new List<Range>();
								break;
							}

							FindNext(true);
						}
						break;
					case "Edit_FindNext":
					case "Edit_FindPrev":
						FindNext(command.Name == "Edit_FindNext");
						break;
					case "Edit_ToUpper":
						{
							var selections = ranges[RangeType.Selection].Where(range => range.HasSelection()).ToList();
							var strs = selections.Select(range => GetString(range).ToUpperInvariant()).ToList();
							Replace(selections, strs, true);
						}
						break;
					case "Edit_ToLower":
						{
							var selections = ranges[RangeType.Selection].Where(range => range.HasSelection()).ToList();
							var strs = selections.Select(range => GetString(range).ToLowerInvariant()).ToList();
							Replace(selections, strs, true);
						}
						break;
					case "Edit_ToHex":
						{
							var selections = ranges[RangeType.Selection].Where(range => range.HasSelection()).ToList();
							var strs = selections.Select(range => Int64.Parse(GetString(range)).ToString("x")).ToList();
							Replace(selections, strs, true);
						}
						break;
					case "Edit_FromHex":
						{
							var selections = ranges[RangeType.Selection].Where(range => range.HasSelection()).ToList();
							var strs = selections.Select(range => Int64.Parse(GetString(range), NumberStyles.HexNumber).ToString()).ToList();
							Replace(selections, strs, true);
						}
						break;
					case "Edit_ToChar":
						{
							var selections = ranges[RangeType.Selection].Where(range => range.HasSelection()).ToList();
							var strs = selections.Select(range => ((char)UInt16.Parse(GetString(range), NumberStyles.HexNumber)).ToString()).ToList();
							Replace(selections, strs, true);
						}
						break;
					case "Edit_FromChar":
						{
							var selections = ranges[RangeType.Selection].Where(range => range.End - range.Start == 1).ToList();
							var strs = selections.Select(range => ((UInt16)GetString(range)[0]).ToString("x2")).ToList();
							Replace(selections, strs, true);
						}
						break;
					case "Edit_Width":
						{
							var minWidth = ranges[RangeType.Selection].Select(range => range.End - range.Start).Max();
							var text = String.Join("", ranges[RangeType.Selection].Where(range => range.HasSelection()).Select(range => GetString(range)));
							var numeric = Regex.IsMatch(text, "^[0-9a-fA-F]+$");
							var widthDialog = new WidthDialog { MinWidthNum = minWidth, PadChar = numeric ? '0' : ' ', Before = numeric };
							if (widthDialog.ShowDialog() == true)
								Replace(ranges[RangeType.Selection], ranges[RangeType.Selection].Select(range => SetWidth(GetString(range), widthDialog.WidthNum, widthDialog.PadChar, widthDialog.Before)).ToList(), true);
						}
						break;
					case "Edit_Trim":
						{
							var selections = ranges[RangeType.Selection].Where(range => range.HasSelection()).ToList();
							var strs = selections.Select(range => GetString(range).Trim().TrimStart('0')).ToList();
							Replace(selections, strs, true);
						}
						break;
					case "Select_Unselect":
						ranges[RangeType.Selection].ForEach(range => range.Pos1 = range.Pos2 = range.Start);
						InvalidateVisual();
						break;
					case "Select_Single":
						ranges[RangeType.Selection] = new List<Range> { ranges[RangeType.Selection].Last() };
						break;
					case "Select_Lines":
						var lines = ranges[RangeType.Selection].SelectMany(selection => Enumerable.Range(Data.GetOffsetLine(selection.Start), Data.GetOffsetLine(selection.End) - Data.GetOffsetLine(selection.Start) + 1)).Distinct().OrderBy(lineNum => lineNum).ToList();
						ranges[RangeType.Selection] = lines.Select(line => new Range { Pos1 = Data.GetOffset(line, 0), Pos2 = Data.GetOffset(line, 0) }).ToList();
						break;
					case "Select_Find":
						ranges[RangeType.Selection] = ranges[RangeType.Search];
						ranges[RangeType.Search] = new List<Range>();
						break;
					case "Select_Marks":
						if (ranges[RangeType.Mark].Count == 0)
							break;

						ranges[RangeType.Selection] = ranges[RangeType.Mark];
						ranges[RangeType.Mark] = new List<Range>();
						break;
					case "Mark_Find":
						ranges[RangeType.Mark].AddRange(ranges[RangeType.Search]);
						ranges[RangeType.Search] = new List<Range>();
						break;
					case "Mark_Selection":
						ranges[RangeType.Mark].AddRange(ranges[RangeType.Selection]);
						ranges[RangeType.Selection] = new List<Range> { ranges[RangeType.Selection].Last().Copy() };
						foreach (var mark in ranges[RangeType.Mark])
							if (!mark.HasSelection())
								mark.Pos1++;
						break;
					case "Mark_Clear":
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
						break;
				}
			}
			catch (Exception ex) { MessageBox.Show(ex.Message, "Error"); }

			InvalidateVisual();
		}

		public bool CommandCanRun(UICommand command, object parameter)
		{
			return true;
		}

		protected override void OnTextInput(TextCompositionEventArgs e)
		{
			base.OnTextInput(e);
			if (e.Handled)
				return;

			if (e.Text.Length == 0)
				return;

			Replace(ranges[RangeType.Selection], ranges[RangeType.Selection].Select(range => e.Text).ToList(), false);
			e.Handled = true;
		}
	}
}
