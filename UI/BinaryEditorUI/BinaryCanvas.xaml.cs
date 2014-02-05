using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NeoEdit.UI.Resources;

namespace NeoEdit.UI.BinaryEditorUI
{
	public partial class BinaryCanvas : Canvas
	{
		[DepProp]
		public byte[] Data { get { return uiHelper.GetPropValue<byte[]>(); } set { uiHelper.SetPropValue(value); } }
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
		[DepProp]
		public long SelStart { get { return uiHelper.GetPropValue<long>(); } set { ++internalChangeCount; uiHelper.SetPropValue(value); --internalChangeCount; } }
		[DepProp]
		public long SelEnd { get { return uiHelper.GetPropValue<long>(); } set { ++internalChangeCount; uiHelper.SetPropValue(value); --internalChangeCount; } }

		int internalChangeCount = 0;
		long _pos1, _pos2;

		long Pos1
		{
			get { return _pos1; }
			set
			{
				_pos1 = Math.Min(Data.Length - 1, Math.Max(0, value));
				if (!selecting)
					_pos2 = _pos1;

				SelStart = Math.Min(_pos1, _pos2);
				SelEnd = Math.Max(_pos1, _pos2);

				EnsureVisible(Pos1);
				InvalidateVisual();
			}
		}

		long Pos2
		{
			get { return _pos2; }
			set
			{
				_pos2 = Math.Min(Data.Length - 1, Math.Max(0, value));

				SelStart = Math.Min(_pos1, _pos2);
				SelEnd = Math.Max(_pos1, _pos2);

				InvalidateVisual();
			}
		}

		[DepProp]
		public bool SelHex { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }

		readonly double charWidth;
		const int minColumns = 4;
		const int maxColumns = Int32.MaxValue;

		bool mouseDown;
		bool overrideSelecting;
		bool shiftDown { get { return (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None; } }
		bool controlDown { get { return (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None; } }
		bool controlOnly { get { return (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)) == ModifierKeys.Control; } }
		bool selecting { get { return (overrideSelecting) || (mouseDown) || (shiftDown); } }

		int columns, rows;

		// X spacing
		const double xStartSpacing = 10;
		const int xPosColumns = 8;
		const int xPosGap = 2;
		const int xHexSpacing = 1;
		const int xHexGap = 2;
		const double xEndSpacing = xStartSpacing;

		double xStart { get { return 0; } }
		double xPosition { get { return xStart + xStartSpacing; } }
		double xHexViewStart { get { return xPosition + (xPosColumns + xPosGap) * charWidth; } }
		double xHexViewEnd { get { return xHexViewStart + (columns * (2 + xHexSpacing) - xHexSpacing) * charWidth; } }
		double xTextViewStart { get { return xHexViewEnd + xHexGap * charWidth; } }
		double xTextViewEnd { get { return xTextViewStart + columns * charWidth; } }
		double xEnd { get { return xTextViewEnd + xEndSpacing; } }

		// Y spacing
		const double yStartSpacing = 10;
		readonly double rowHeight;
		const double yEndSpacing = yStartSpacing;

		double yStart { get { return 0; } }
		double yLinesStart { get { return yStart + yStartSpacing; } }
		double yLinesEnd { get { return yLinesStart + rows * rowHeight; } }
		double yEnd { get { return yLinesEnd + yEndSpacing; } }

		readonly Typeface typeface;
		readonly double fontSize;

		static BinaryCanvas() { UIHelper<BinaryCanvas>.Register(); }

		readonly UIHelper<BinaryCanvas> uiHelper;
		public BinaryCanvas()
		{
			uiHelper = new UIHelper<BinaryCanvas>(this);
			InitializeComponent();

			var fontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./UI/Resources/#Anonymous Pro");
			typeface = fontFamily.GetTypefaces().First();
			fontSize = 14;
			rowHeight = fontSize;

			var example = "0123456789 abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ !@#$%^&*()";
			var formattedText = new FormattedText(example, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
			charWidth = formattedText.Width / example.Length;

			uiHelper.AddCallback(a => a.Data, (o, n) => InvalidateVisual());
			uiHelper.AddCallback(a => a.SelHex, (o, n) => InvalidateVisual());
			uiHelper.AddCallback(Canvas.ActualWidthProperty, this, () => InvalidateVisual());
			uiHelper.AddCallback(Canvas.ActualHeightProperty, this, () => InvalidateVisual());
			uiHelper.AddCallback(a => a.xScrollValue, (o, n) => InvalidateVisual());
			uiHelper.AddCallback(a => a.yScrollValue, (o, n) => InvalidateVisual());

			uiHelper.AddCallback(a => a.SelStart, (o, n) =>
			{
				if (internalChangeCount != 0)
					return;

				Pos2 = SelStart;
				EnsureVisible(Pos2);
			});
			uiHelper.AddCallback(a => a.SelEnd, (o, n) =>
			{
				if (internalChangeCount != 0)
					return;

				overrideSelecting = true;
				Pos1 = SelEnd;
				overrideSelecting = false;
			});

			Loaded += (s, e) => InvalidateVisual();
		}

		void EnsureVisible(long position)
		{
			var y = GetYFromRow(position / columns);
			yScrollValue = Math.Min(y, Math.Max(y + rowHeight - ActualHeight, yScrollValue));
		}

		long GetRowFromY(double y)
		{
			return (long)((y - yLinesStart) / rowHeight);
		}

		double GetYFromRow(long row)
		{
			return yLinesStart + row * rowHeight;
		}

		double GetXHexFromColumn(long column)
		{
			return xHexViewStart + column * (2 + xHexSpacing) * charWidth;
		}

		long GetColumnFromXHex(double x)
		{
			return (long)((x - xHexViewStart) / (2 + xHexSpacing) / charWidth);
		}

		double GetXTextFromColumn(long column)
		{
			return xTextViewStart + column * charWidth;
		}

		long GetColumnFromXText(double x)
		{
			return (long)((x - xTextViewStart) / charWidth);
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			base.OnRender(drawingContext);

			if (Data == null)
				return;

			columns = Math.Min(maxColumns, Math.Max(minColumns, ((int)((ActualWidth - xStartSpacing - xEndSpacing) / charWidth) - xPosColumns - xPosGap - xHexGap + xHexSpacing) / (3 + xHexSpacing)));
			rows = (Data.Length + columns - 1) / columns;

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
				var y = yLinesStart - yScrollValue + row * rowHeight;
				var selected = new bool[columns];
				string hex = "", text = "";
				var useColumns = Math.Min(columns, Data.Length - row * columns);
				for (var column = 0; column < useColumns; ++column)
				{
					var pos = row * columns + column;
					if ((pos >= SelStart) && (pos <= SelEnd))
						selected[column] = true;

					var b = Data[pos];
					var c = (char)b;

					hex += String.Format("{0:x2}", b) + new string(' ', xHexSpacing);
					text += Char.IsControl(c) ? '·' : c;
				}

				var posText = new FormattedText(String.Format("{0:x" + xPosColumns.ToString() + "}", row * columns), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
				var hexText = new FormattedText(hex, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
				var textText = new FormattedText(text, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);

				for (var first = 0; first < selected.Length; first++)
				{
					if (!selected[first])
						continue;

					int last;
					for (last = first; last < selected.Length; last++)
					{
						if (!selected[last])
							break;
						selected[last] = false;
					}

					var count = last - first;

					hexText.SetForegroundBrush(Brushes.White, first * (xHexSpacing + 2), count * (2 + xHexSpacing));
					drawingContext.DrawRectangle(SelHex ? Brushes.Blue : Brushes.Gray, null, new Rect(GetXHexFromColumn(first) - xScrollValue, y, (count * (2 + xHexSpacing) - xHexSpacing) * charWidth, rowHeight));

					textText.SetForegroundBrush(Brushes.White, first, count);
					drawingContext.DrawRectangle(SelHex ? Brushes.Gray : Brushes.Blue, null, new Rect(GetXTextFromColumn(first) - xScrollValue, y, count * charWidth, rowHeight));
				}

				drawingContext.DrawText(posText, new Point(xPosition - xScrollValue, y));
				drawingContext.DrawText(hexText, new Point(xHexViewStart - xScrollValue, y));
				drawingContext.DrawText(textText, new Point(xTextViewStart - xScrollValue, y));
			}
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			e.Handled = true;
			switch (e.Key)
			{
				case Key.Tab: SelHex = !SelHex; break;
				case Key.Up: Pos1 -= columns; break;
				case Key.Down: Pos1 += columns; break;
				case Key.Left: --Pos1; break;
				case Key.Right: ++Pos1; break;
				case Key.Home:
					if (controlDown)
						Pos1 = 0;
					else
						Pos1 -= Pos1 % columns;
					break;
				case Key.End:
					if (controlDown)
						Pos1 = Data.Length;
					else
						Pos1 += columns - Pos1 % columns - 1;
					break;
				case Key.PageUp:
					if (controlDown)
						Pos1 -= (Pos1 / columns - GetRowFromY(yScrollValue + rowHeight - 1)) * columns;
					else
						Pos1 -= (long)(ActualHeight / rowHeight - 1) * columns;
					break;
				case Key.PageDown:
					if (controlDown)
						Pos1 += (GetRowFromY(ActualHeight + yScrollValue - rowHeight) - Pos1 / columns) * columns;
					else
						Pos1 += (long)(ActualHeight / rowHeight - 1) * columns;
					break;
				case Key.A:
					if (controlOnly)
					{
						Pos1 = Data.Length;
						Pos2 = 0;
					}
					break;
				default: e.Handled = false; break;
			}
		}

		void MouseHandler(Point mousePos)
		{
			var x = mousePos.X + xScrollValue;
			var row = GetRowFromY(mousePos.Y + yScrollValue);
			long column;
			bool isHex;

			if ((x >= xHexViewStart) && (x <= xHexViewEnd))
			{
				isHex = true;
				column = GetColumnFromXHex(x);
			}
			else if ((x >= xTextViewStart) && (x <= xTextViewEnd))
			{
				isHex = false;
				column = GetColumnFromXText(x);
			}
			else
				return;

			if ((column < 0) || (column >= columns))
				return;

			var pos = row * columns + column;
			if ((pos < 0) || (pos > Data.Length))
				return;

			SelHex = isHex;
			Pos1 = pos;
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
