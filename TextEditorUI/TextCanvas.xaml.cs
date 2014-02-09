using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NeoEdit.Common;

namespace NeoEdit.TextEditorUI
{
	class Selection
	{
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

		public override string ToString()
		{
			return String.Format("({0},{1})->({2},{3})", StartRow, StartIndex, EndRow, EndIndex);
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

		List<Selection> selections = new List<Selection>();

		readonly Typeface typeface;
		readonly double fontSize;

		static TextCanvas() { UIHelper<TextCanvas>.Register(); }

		readonly UIHelper<TextCanvas> uiHelper;
		public TextCanvas()
		{
			uiHelper = new UIHelper<TextCanvas>(this);
			InitializeComponent();

			selections.Add(new Selection());

			var fontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./Resources/#Anonymous Pro");
			typeface = fontFamily.GetTypefaces().First();
			fontSize = 14;
			rowHeight = fontSize;

			var example = "0123456789 abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ !@#$%^&*()";
			var formattedText = new FormattedText(example, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
			charWidth = formattedText.Width / example.Length;

			uiHelper.AddCallback(a => a.Data, (o, n) => InvalidateVisual());
			uiHelper.AddCallback(a => a.ChangeCount, (o, n) => InvalidateVisual());
			uiHelper.AddCallback(a => a.xScrollValue, (o, n) => InvalidateVisual());
			uiHelper.AddCallback(a => a.yScrollValue, (o, n) => InvalidateVisual());
			uiHelper.AddCallback(Canvas.ActualWidthProperty, this, () => InvalidateVisual());
			uiHelper.AddCallback(Canvas.ActualHeightProperty, this, () => { EnsureVisible(selections.First()); InvalidateVisual(); });

			Loaded += (s, e) => InvalidateVisual();
		}

		internal void EnsureVisible(Selection selection)
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

		Brush selectionBrush = new SolidColorBrush(Color.FromRgb(173, 214, 255));
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

			var startRow = Math.Max(0, GetRowFromY(yScrollValue));
			var endRow = Math.Min(rows, GetRowFromY(ActualHeight + rowHeight + yScrollValue));

			for (var row = startRow; row < endRow; ++row)
			{
				var line = Data[row];
				var y = yLinesStart - yScrollValue + row * rowHeight;

				foreach (var selection in selections)
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

					var startColumn = GetColumnFromIndex(line, startIndex);
					var endColumn = GetColumnFromIndex(line, endIndex);

					dc.DrawRectangle(selectionBrush, null, new Rect(GetXFromColumn(startColumn) - xScrollValue, y, (endColumn - startColumn) * charWidth, rowHeight));
				}

				foreach (var selection in selections)
				{
					if (selection.Pos1Row == row)
					{
						var column = GetColumnFromIndex(line, selection.Pos1Index);
						dc.DrawRectangle(Brushes.Black, null, new Rect(GetXFromColumn(column) - xScrollValue, y, 1, rowHeight));
					}
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
				case Key.Left:
					foreach (var selection in selections)
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
					foreach (var selection in selections)
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
							if (selections.Count == 1)
							{
								var adj = Math.Min(0, selections[0].Pos1Row - GetRowFromY(yScrollValue + rowHeight - 1)) + Math.Max(0, selections[0].Pos1Row - GetRowFromY(yScrollValue + ActualHeight - rowHeight));
								SetPos1(selections[0], selections[0].Pos1Row - adj, selections[0].Pos1Index);

							}
						}
						else
							foreach (var selection in selections)
								SetPos1(selection, selection.Pos1Row + mult, selection.Pos1Index);
					}
					break;
				case Key.Home:
					if (controlDown)
					{
						foreach (var selection in selections)
							SetPos1(selection, 0, 0);
					}
					else
					{
						bool changed = false;
						foreach (var selection in selections)
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
							foreach (var selection in selections)
								SetPos1(selection, selection.Pos1Row, 0);
							xScrollValue = 0;
						}
					}
					break;
				case Key.End:
					foreach (var selection in selections)
						if (controlDown)
							SetPos1(selection, Data.NumLines - 1, Data[Data.NumLines - 1].Length);
						else
							SetPos1(selection, selection.Pos1Row, Data[selection.Pos1Row].Length);
					break;
				case Key.PageUp:
					foreach (var selection in selections)
						if (controlDown)
							SetPos1(selection, GetRowFromY(yScrollValue + rowHeight - 1), selection.Pos1Index);
						else
							SetPos1(selection, selection.Pos1Row - (int)(ActualHeight / rowHeight - 1), selection.Pos1Index);
					break;
				case Key.PageDown:
					foreach (var selection in selections)
						if (controlDown)
							SetPos1(selection, GetRowFromY(ActualHeight + yScrollValue - rowHeight), selection.Pos1Index);
						else
							SetPos1(selection, selection.Pos1Row + (int)(ActualHeight / rowHeight - 1), selection.Pos1Index);
					break;
				case Key.A:
					if (controlDown)
					{
						foreach (var selection in selections)
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

			ConsolidateSelections();
		}

		enum WordSkipType
		{
			None,
			Char,
			Symbol,
			Space,
		}

		void MoveNextWord(Selection selection)
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

		void MovePrevWord(Selection selection)
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

		void SetPos1(Selection selection, int row, int index)
		{
			selection.Pos1Row = Math.Max(0, Math.Min(row, Data.NumLines - 1));
			selection.Pos1Index = Math.Max(0, Math.Min(index, Data[selection.Pos1Row].Length));

			if (!selecting)
				SetPos2(selection, row, index);

			EnsureVisible(selection);
			InvalidateVisual();
		}

		void SetPos2(Selection selection, int row, int index)
		{
			selection.Pos2Row = Math.Max(0, Math.Min(row, Data.NumLines - 1));
			selection.Pos2Index = Math.Max(0, Math.Min(index, Data[selection.Pos2Row].Length));
			InvalidateVisual();
		}

		void ConsolidateSelections()
		{
			selections = selections.GroupBy(a => a.ToString()).Select(a => a.First()).ToList();
		}

		void MouseHandler(Point mousePos)
		{
			var row = GetRowFromY(mousePos.Y + yScrollValue);
			var index = GetIndexFromColumn(row, GetColumnFromX(mousePos.X + xScrollValue));

			Selection selection;
			if (selecting)
				selection = selections.Last();
			else
			{
				if (!controlDown)
					selections.Clear();

				selection = new Selection();
				selections.Add(selection);
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
	}
}
