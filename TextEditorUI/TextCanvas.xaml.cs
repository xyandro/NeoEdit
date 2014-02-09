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
			return new Range { Pos1Row = Pos1Row, Pos1Index = Pos1Index, Pos2Row = Pos2Row, Pos2Index = Pos2Index };
		}

		public int Pos1Row { get; set; }
		public int Pos1Index { get; set; }
		public int Pos2Row { get; set; }
		public int Pos2Index { get; set; }
		public int StartRow { get { return Math.Min(Pos1Row, Pos2Row); } }
		public int EndRow { get { return Math.Max(Pos1Row, Pos2Row); } }
		public int StartIndex
		{
			get
			{
				if (Pos1Row < Pos2Row)
					return Pos1Index;
				else if (Pos1Row > Pos2Row)
					return Pos2Index;
				return Math.Min(Pos1Index, Pos2Index);
			}
		}
		public int EndIndex
		{
			get
			{
				if (Pos1Row > Pos2Row)
					return Pos1Index;
				else if (Pos1Row < Pos2Row)
					return Pos2Index;
				return Math.Max(Pos1Index, Pos2Index);
			}
		}

		public bool HasSelection()
		{
			return (Pos1Row != Pos2Row) || (Pos1Index != Pos2Index);
		}

		public void RemoveSelection()
		{
			var row = StartRow;
			var index = StartIndex;
			Pos1Row = Pos2Row = row;
			Pos1Index = Pos2Index = index;
		}

		public bool InRow(int row)
		{
			return (row >= StartRow) && (row <= EndRow);
		}

		public int RowStartIndex(int row)
		{
			if (row == StartRow)
				return StartIndex;
			return 0;
		}

		public int RowEndIndex(int row, int max)
		{
			if (row == EndRow)
				return EndIndex;
			return max;
		}

		public bool Overlaps(Range range)
		{
			if (StartRow > range.EndRow)
				return false;
			if ((StartRow == range.EndRow) && (StartIndex > range.EndIndex))
				return false;
			if (EndRow < range.StartRow)
				return false;
			if ((EndRow == range.StartRow) && (EndIndex <= range.StartIndex))
				return false;
			return true;
		}

		public void Extend(Range range)
		{
			var first = ((StartRow < range.StartRow) || ((StartRow == range.StartRow) && (StartIndex < range.StartIndex))) ? this : range;
			var last = ((EndRow > range.EndRow) || ((EndRow == range.EndRow) && (EndIndex > range.EndIndex))) ? this : range;
			var startRow = first.StartRow;
			var startIndex = first.StartIndex;
			var endRow = last.EndRow;
			var endIndex = last.EndIndex;
			Pos1Row = startRow;
			Pos1Index = startIndex;
			Pos2Row = endRow;
			Pos2Index = endIndex;
		}

		public override string ToString()
		{
			return String.Format("({0:0000000000},{1:0000000000})->({2:0000000000},{3:0000000000})", StartRow, StartIndex, EndRow, EndIndex);
		}
	};

	public partial class TextCanvas : Canvas
	{
		[DepProp]
		public TextData Data { get { return uiHelper.GetPropValue<TextData>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public long ChangeCount { get { return uiHelper.GetPropValue<long>(); } set { uiHelper.SetPropValue(value); } }
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

		int rows, columns;

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
		readonly double rowHeight;
		const double yEndSpacing = yStartSpacing;

		double yStart { get { return 0; } }
		double yLinesStart { get { return yStart + yStartSpacing; } }
		double yLinesEnd { get { return yLinesStart + rows * rowHeight; } }
		double yEnd { get { return yLinesEnd + yEndSpacing; } }

		enum RangeType
		{
			Search,
			Mark,
			Selection,
		}
		Dictionary<RangeType, List<Range>> ranges = Helpers.GetValues<RangeType>().ToDictionary(a => a, a => new List<Range>());

		readonly Typeface typeface;
		readonly double fontSize;

		static TextCanvas() { UIHelper<TextCanvas>.Register(); }

		readonly UIHelper<TextCanvas> uiHelper;
		public TextCanvas()
		{
			uiHelper = new UIHelper<TextCanvas>(this);
			InitializeComponent();

			ranges[RangeType.Selection].Add(new Range());

			var fontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./Resources/#Anonymous Pro");
			typeface = fontFamily.GetTypefaces().First();
			fontSize = 14;
			rowHeight = fontSize;

			var example = "0123456789 abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ !@#$%^&*()";
			var formattedText = new FormattedText(example, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
			charWidth = formattedText.Width / example.Length;

			uiHelper.AddCallback(a => a.Data, (o, n) => InvalidateVisual());
			uiHelper.AddCallback(a => a.ChangeCount, (o, n) => { ranges[RangeType.Search].Clear(); InvalidateVisual(); });
			uiHelper.AddCallback(a => a.xScrollValue, (o, n) => InvalidateVisual());
			uiHelper.AddCallback(a => a.yScrollValue, (o, n) => InvalidateVisual());
			uiHelper.AddCallback(Canvas.ActualWidthProperty, this, () => InvalidateVisual());
			uiHelper.AddCallback(Canvas.ActualHeightProperty, this, () => { EnsureVisible(ranges[RangeType.Selection].First()); InvalidateVisual(); });

			Loaded += (s, e) => InvalidateVisual();
		}

		internal void EnsureVisible(Range selection)
		{
			var y = GetYFromRow(selection.Pos1Row);
			yScrollValue = Math.Min(y, Math.Max(y + rowHeight - ActualHeight, yScrollValue));
			var x = GetXFromRowIndex(selection.Pos1Row, selection.Pos1Index);
			xScrollValue = Math.Min(x, Math.Max(x + charWidth - ActualWidth, xScrollValue));
		}

		double GetXFromRowIndex(int row, int index)
		{
			var column = GetColumnFromIndex(row, index);
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

		int GetRowFromY(double y)
		{
			return (int)((y - yLinesStart) / rowHeight);
		}

		double GetYFromRow(int row)
		{
			return yLinesStart + row * rowHeight;
		}

		int GetColumnFromIndex(int row, int index)
		{
			return GetColumnFromIndex(Data[row], index);
		}

		int GetColumnFromIndex(string line, int findIndex)
		{
			if ((findIndex < 0) || (findIndex > line.Length))
				throw new IndexOutOfRangeException();

			var column = 0;
			var index = 0;
			while (index < findIndex)
			{
				var find = line.IndexOf('\t', index);
				if (find == index)
				{
					column = (column / tabStop + 1) * tabStop;
					++index;
					continue;
				}

				if (find == -1)
					find = line.Length;
				find = Math.Min(find, findIndex);

				column += find - index;
				index = find;
			}
			return column;
		}

		int GetIndexFromColumn(int row, int index)
		{
			return GetIndexFromColumn(Data[row], index);
		}

		int GetIndexFromColumn(string line, int findColumn)
		{
			if (findColumn < 0)
				throw new IndexOutOfRangeException();

			var column = 0;
			var index = 0;
			while (index < line.Length)
			{
				if (column >= findColumn)
					break;

				var find = line.IndexOf('\t', index);
				if (find == index)
				{
					column = (column / tabStop + 1) * tabStop;
					++index;
					continue;
				}

				if (find == -1)
					find = line.Length;
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

			columns = Enumerable.Range(0, Data.NumLines).Select(a => Data[a]).Select(a => GetColumnFromIndex(a, a.Length)).Max();
			rows = Data.NumLines;

			xScrollMaximum = xEnd - ActualWidth;
			xScrollSmallChange = charWidth;
			xScrollLargeChange = ActualWidth - xScrollSmallChange;

			yScrollMaximum = yEnd - ActualHeight;
			yScrollSmallChange = rowHeight;
			yScrollLargeChange = ActualHeight - yScrollSmallChange;

			for (var ctr1 = 0; ctr1 < ranges[RangeType.Mark].Count; ++ctr1)
			{
				var ctr2 = ctr1 + 1;
				while (ctr2 < ranges[RangeType.Mark].Count)
				{
					if (ranges[RangeType.Mark][ctr1].Overlaps(ranges[RangeType.Mark][ctr2]))
					{
						ranges[RangeType.Mark][ctr1].Extend(ranges[RangeType.Mark][ctr2]);
						ranges[RangeType.Mark].RemoveAt(ctr2);
					}
					else
						++ctr2;
				}
			}

			ranges[RangeType.Selection] = ranges[RangeType.Selection].GroupBy(a => a.ToString()).OrderBy(a => a.Key).Select(a => a.First()).ToList();

			var startRow = Math.Max(0, GetRowFromY(yScrollValue));
			var endRow = Math.Min(rows, GetRowFromY(ActualHeight + rowHeight + yScrollValue));

			for (var row = startRow; row < endRow; ++row)
			{
				var line = Data[row];
				var y = yLinesStart - yScrollValue + row * rowHeight;

				foreach (var entry in ranges)
				{
					foreach (var range in entry.Value)
					{
						if (!range.InRow(row))
							continue;

						var startColumn = GetColumnFromIndex(line, range.RowStartIndex(row));
						var endColumn = GetColumnFromIndex(line, range.RowEndIndex(row, line.Length));

						dc.DrawRectangle(brushes[entry.Key], null, new Rect(GetXFromColumn(startColumn) - xScrollValue, y, (endColumn - startColumn) * charWidth, rowHeight));
					}
				}

				foreach (var selection in ranges[RangeType.Selection])
				{
					if (selection.Pos1Row != row)
						continue;

					var column = GetColumnFromIndex(line, selection.Pos1Index);
					dc.DrawRectangle(Brushes.Black, null, new Rect(GetXFromColumn(column) - xScrollValue, y, 1, rowHeight));
				}

				var index = 0;
				var sb = new StringBuilder();
				while (index < line.Length)
				{
					var find = line.IndexOf('\t', index);
					if (find == index)
					{
						sb.Append(' ', (sb.Length / tabStop + 1) * tabStop - sb.Length);
						++index;
						continue;
					}

					if (find == -1)
						find = line.Length;
					sb.Append(line, index, find - index);
					index = find;
				}

				var text = new FormattedText(sb.ToString(), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
				dc.DrawText(text, new Point(xLineStart - xScrollValue, y));
			}
		}

		bool mouseDown;
		bool? overrideSelecting;
		bool shiftDown { get { return (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None; } }
		bool controlDown { get { return (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None; } }
		internal bool selecting { get { return overrideSelecting.HasValue ? overrideSelecting.Value : ((mouseDown) || (shiftDown)); } }

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
					foreach (var selection in GetReverseOrderSelections())
					{
						if (!selection.HasSelection())
						{
							var row = selection.StartRow;
							var index = selection.StartIndex;

							if (e.Key == Key.Back)
								--index;
							else
								++index;

							if (index < 0)
							{
								--row;
								if (row < 0)
									continue;
								index = Data[row].Length;
							}
							if (index > Data[row].Length)
							{
								++row;
								if (row >= Data.NumLines)
									continue;
								index = 0;
							}

							selection.Pos1Row = row;
							selection.Pos1Index = index;
						}

						Delete(selection);
					}
					break;
				case Key.Escape: ranges[RangeType.Search].Clear(); break;
				case Key.Left:
					foreach (var selection in ranges[RangeType.Selection])
					{
						if (controlDown)
							MovePrevWord(selection);
						else if ((selection.Pos1Index == 0) && (selection.Pos1Row != 0))
							SetPos1(selection, selection.Pos1Row - 1, Data[selection.Pos1Row - 1].Length);
						else
							SetPos1(selection, selection.Pos1Row, selection.Pos1Index - 1);
					}
					break;
				case Key.Right:
					foreach (var selection in ranges[RangeType.Selection])
					{
						if (controlDown)
							MoveNextWord(selection);
						else if ((selection.Pos1Index == Data[selection.Pos1Row].Length) && (selection.Pos1Row != Data.NumLines - 1))
							SetPos1(selection, selection.Pos1Row + 1, 0);
						else
							SetPos1(selection, selection.Pos1Row, selection.Pos1Index + 1);
					}
					break;
				case Key.Up:
				case Key.Down:
					{
						var mult = e.Key == Key.Up ? -1 : 1;
						if (controlDown)
						{
							yScrollValue += rowHeight * mult;
							if (ranges[RangeType.Selection].Count == 1)
							{
								var adj = Math.Min(0, ranges[RangeType.Selection][0].Pos1Row - GetRowFromY(yScrollValue + rowHeight - 1)) + Math.Max(0, ranges[RangeType.Selection][0].Pos1Row - GetRowFromY(yScrollValue + ActualHeight - rowHeight));
								SetPos1(ranges[RangeType.Selection][0], ranges[RangeType.Selection][0].Pos1Row - adj, ranges[RangeType.Selection][0].Pos1Index);

							}
						}
						else
							foreach (var selection in ranges[RangeType.Selection])
								SetPos1(selection, selection.Pos1Row + mult, selection.Pos1Index);
					}
					break;
				case Key.Home:
					if (controlDown)
					{
						foreach (var selection in ranges[RangeType.Selection])
							SetPos1(selection, 0, 0);
					}
					else
					{
						bool changed = false;
						foreach (var selection in ranges[RangeType.Selection])
						{
							int index;
							var line = Data[selection.Pos1Row];
							for (index = 0; index < line.Length; ++index)
							{
								if ((line[index] != ' ') && (line[index] != '\t'))
									break;
							}
							if (index != selection.Pos1Index)
								changed = true;
							SetPos1(selection, selection.Pos1Row, index);
						}
						if (!changed)
						{
							foreach (var selection in ranges[RangeType.Selection])
								SetPos1(selection, selection.Pos1Row, 0);
							xScrollValue = 0;
						}
					}
					break;
				case Key.End:
					foreach (var selection in ranges[RangeType.Selection])
						if (controlDown)
							SetPos1(selection, Data.NumLines - 1, Data[Data.NumLines - 1].Length);
						else
							SetPos1(selection, selection.Pos1Row, Data[selection.Pos1Row].Length);
					break;
				case Key.PageUp:
					foreach (var selection in ranges[RangeType.Selection])
						if (controlDown)
							SetPos1(selection, GetRowFromY(yScrollValue + rowHeight - 1), selection.Pos1Index);
						else
							SetPos1(selection, selection.Pos1Row - (int)(ActualHeight / rowHeight - 1), selection.Pos1Index);
					break;
				case Key.PageDown:
					foreach (var selection in ranges[RangeType.Selection])
						if (controlDown)
							SetPos1(selection, GetRowFromY(ActualHeight + yScrollValue - rowHeight), selection.Pos1Index);
						else
							SetPos1(selection, selection.Pos1Row + (int)(ActualHeight / rowHeight - 1), selection.Pos1Index);
					break;
				case Key.A:
					if (controlDown)
					{
						foreach (var selection in ranges[RangeType.Selection])
						{
							SetPos1(selection, Data.NumLines - 1, Data[Data.NumLines - 1].Length);
							SetPos2(selection, 0, 0);
						}
					}
					else
						e.Handled = false;
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

			var row = selection.Pos1Row;
			var index = selection.Pos1Index - 1;
			string line = null;
			int lineIndex = -1;
			while (true)
			{
				if (lineIndex != row)
				{
					line = Data[row];
					lineIndex = row;
				}

				++index;
				WordSkipType current;
				if ((index >= line.Length) || (line[index] == ' ') || (line[index] == '\t'))
					current = WordSkipType.Space;
				else if ((Char.IsLetterOrDigit(line[index])) || (line[index] == '_'))
					current = WordSkipType.Char;
				else
					current = WordSkipType.Symbol;

				if ((moveType == WordSkipType.None) || (current == WordSkipType.Space))
					moveType = current;

				if (index >= line.Length)
				{
					index = -1;
					row++;
					if (row >= Data.NumLines)
						return;
					continue;
				}

				if ((current != WordSkipType.Space) && (current != moveType))
				{
					SetPos1(selection, row, index);
					return;
				}
			}
		}

		void MovePrevWord(Range selection)
		{
			WordSkipType moveType = WordSkipType.None;

			var row = selection.Pos1Row;
			var index = selection.Pos1Index;
			int lastRow = -1, lastIndex = -1;
			string line = null;
			int lineIndex = -1;
			while (true)
			{
				if (lineIndex != row)
				{
					line = Data[row];
					lineIndex = row;
					if (index < 0)
						index = line.Length;
				}

				lastRow = row;
				lastIndex = index;

				--index;
				WordSkipType current;
				if ((index < 0) || (line[index] == ' ') || (line[index] == '\t'))
					current = WordSkipType.Space;
				else if ((Char.IsLetterOrDigit(line[index])) || (line[index] == '_'))
					current = WordSkipType.Char;
				else
					current = WordSkipType.Symbol;

				if ((moveType == WordSkipType.None) || (moveType == WordSkipType.Space))
					moveType = current;

				if (index < 0)
				{
					--row;
					if (row < 0)
					{
						SetPos1(selection, 0, 0);
						return;
					}
					continue;
				}

				if (current != moveType)
				{
					SetPos1(selection, lastRow, lastIndex);
					return;
				}
			}
		}

		void SetPos1(Range selection, int row, int index)
		{
			selection.Pos1Row = Math.Max(0, Math.Min(row, Data.NumLines - 1));
			selection.Pos1Index = Math.Max(0, Math.Min(index, Data[selection.Pos1Row].Length));

			if (!selecting)
				SetPos2(selection, row, index);

			EnsureVisible(selection);
			InvalidateVisual();
		}

		void SetPos2(Range selection, int row, int index)
		{
			selection.Pos2Row = Math.Max(0, Math.Min(row, Data.NumLines - 1));
			selection.Pos2Index = Math.Max(0, Math.Min(index, Data[selection.Pos2Row].Length));
			InvalidateVisual();
		}

		void MouseHandler(Point mousePos)
		{
			var row = GetRowFromY(mousePos.Y + yScrollValue);
			var index = GetIndexFromColumn(row, GetColumnFromX(mousePos.X + xScrollValue));

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
			SetPos1(selection, row, index);
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

			for (var row = 0; row < Data.NumLines; row++)
			{
				var line = Data[row];
				var matches = regex.Matches(line);
				foreach (Match match in matches)
				{
					if (selectionOnly)
					{
						var foundMatch = false;
						foreach (var selection in ranges[RangeType.Selection])
						{
							int startIndex;
							if (selection.StartRow < row)
								startIndex = 0;
							else if (selection.StartRow == row)
								startIndex = selection.StartIndex;
							else
								continue;

							int endIndex;
							if (selection.EndRow > row)
								endIndex = line.Length;
							else if (selection.EndRow == row)
								endIndex = selection.EndIndex;
							else
								continue;

							if ((match.Index >= startIndex) && (match.Index + match.Length <= endIndex))
							{
								foundMatch = true;
								break;
							}
						}
						if (!foundMatch)
							continue;
					}

					ranges[RangeType.Search].Add(new Range { Pos1Row = row, Pos1Index = match.Index, Pos2Row = row, Pos2Index = match.Index + match.Length });
				}
			}
			InvalidateVisual();
		}

		string GetString(Range range)
		{
			return Data.GetString(range.StartRow, range.StartIndex, range.EndRow, range.EndIndex);
		}

		void Delete(Range range)
		{
			Data.Delete(range.StartRow, range.StartIndex, range.EndRow, range.EndIndex);
			range.RemoveSelection();
			InvalidateVisual();
		}

		void Insert(Range range, string text)
		{
			if (range.HasSelection())
				Delete(range);
			Data.Insert(range.StartRow, range.StartIndex, text);
			range.Pos1Index += text.Length;
			range.Pos2Index += text.Length;
			InvalidateVisual();
		}

		public void CommandRun(UICommand command, object parameter)
		{
			switch (command.Name)
			{
				case "Edit_Cut":
				case "Edit_Copy":
					{
						var result = ranges[RangeType.Selection].Where(range => range.HasSelection()).Select(range => GetString(range)).ToArray();
						if (result.Length != 0)
							Clipboard.Current.Set(result);
						if (command.Name == "Edit_Cut")
							foreach (var selection in GetReverseOrderSelections())
								Delete(selection);
					}
					break;
				case "Edit_Paste":
					{
						var result = Clipboard.Current.GetStrings().ToList();
						if ((result == null) || (result.Count == 0))
							break;

						var sels = GetReverseOrderSelections();
						while (result.Count > sels.Count)
						{
							result[result.Count - 2] += " " + result[result.Count - 1];
							result.RemoveAt(result.Count - 1);
						}
						while (result.Count < sels.Count)
							result.Add(result.Last());

						result.Reverse();
						for (var ctr = 0; ctr < sels.Count; ++ctr)
							Insert(sels[ctr], result[ctr]);
					}
					break;
				case "Edit_Find":
					try
					{
						Regex regex;
						bool selectionOnly;
						FindDialog.Run(out regex, out selectionOnly);
						RunSearch(regex, selectionOnly);
					}
					catch (Exception ex) { MessageBox.Show(ex.Message, "Error"); }
					break;
				case "Edit_MarkFinds":
					ranges[RangeType.Mark].AddRange(ranges[RangeType.Search]);
					ranges[RangeType.Search] = new List<Range>();
					break;
				case "Selection_Single":
					ranges[RangeType.Selection] = new List<Range> { ranges[RangeType.Selection].Last() };
					break;
				case "Selection_Lines":
					var lines = ranges[RangeType.Selection].SelectMany(selection => Enumerable.Range(selection.StartRow, selection.EndRow - selection.StartRow + 1)).Distinct().OrderBy(a => a).ToList();
					lines = lines.Where(line => Data[line].Length != 0).ToList();
					ranges[RangeType.Selection] = lines.Select(row => { var sel = new Range(); SetPos1(sel, row, 0); return sel; }).ToList();
					break;
				case "Selection_SearchResults":
					ranges[RangeType.Selection] = ranges[RangeType.Search];
					ranges[RangeType.Search] = new List<Range>();
					break;
				case "Selection_Marks":
					if (ranges[RangeType.Mark].Count != 0)
					{
						ranges[RangeType.Selection] = ranges[RangeType.Mark];
						ranges[RangeType.Mark] = new List<Range>();
					}
					break;
				case "Selection_Mark":
					foreach (var selection in ranges[RangeType.Selection])
						ranges[RangeType.Mark].Add(selection.Copy());
					break;
				case "Selection_ClearMarks": ranges[RangeType.Mark].Clear(); break;
			}
			InvalidateVisual();
		}

		public bool CommandCanRun(UICommand command, object parameter)
		{
			return true;
		}

		List<Range> GetReverseOrderSelections()
		{
			return ranges[RangeType.Selection].OrderByDescending(a => a.ToString()).ToList();
		}

		protected override void OnTextInput(TextCompositionEventArgs e)
		{
			base.OnTextInput(e);
			if (e.Handled)
				return;

			if (e.Text.Length == 0)
				return;

			foreach (var selection in GetReverseOrderSelections())
				Insert(selection, e.Text);
			e.Handled = true;
		}
	}
}
