using System;
using System.Globalization;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using NeoEdit.UI.Resources;

namespace NeoEdit.UI.BinaryEditorUI
{
	public partial class BinaryEditor : Window
	{
		[DepProp]
		public byte[] Data { get { return uiHelper.GetPropValue<byte[]>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public FontFamily CustomFontFamily { get { return uiHelper.GetPropValue<FontFamily>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public double CustomFontSize { get { return uiHelper.GetPropValue<double>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public long SelStart
		{
			get { return uiHelper.GetPropValue<long>(); }
			set
			{
				value = Math.Min(Data.Length - 1, Math.Max(0, value));
				uiHelper.SetPropValue(value);
			}
		}
		[DepProp]
		public long SelEnd
		{
			get { return uiHelper.GetPropValue<long>(); }
			set
			{
				value = Math.Min(Data.Length - 1, Math.Max(0, value));
				if (!selecting)
					SelStart = value;

				uiHelper.SetPropValue(value);
			}
		}
		[DepProp]
		public bool SelHex { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }

		readonly double charWidth;
		const int minColumns = 4;
		const int maxColumns = Int32.MaxValue;

		bool mouseDown;
		bool shiftDown { get { return (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None; } }
		bool controlDown { get { return (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None; } }
		bool controlOnly { get { return (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)) == ModifierKeys.Control; } }
		bool selecting { get { return (mouseDown) || (shiftDown); } }

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

		readonly UIHelper<BinaryEditor> uiHelper;
		public BinaryEditor(byte[] data)
		{
			InitializeComponent();
			uiHelper = new UIHelper<BinaryEditor>(this);

			Data = data;
			SelStart = SelEnd = 0;
			SelHex = false;

			CustomFontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./UI/Resources/#Anonymous Pro");
			CustomFontSize = 14;
			rowHeight = CustomFontSize;

			var example = "0123456789 abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ !@#$%^&*()";
			var formattedText = new FormattedText(example, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, CustomFontFamily.GetTypefaces().First(), CustomFontSize, Brushes.Black);
			charWidth = formattedText.Width / example.Length;

			uiHelper.AddCallback(a => a.Data, (o, n) => ScheduleLayout());
			uiHelper.AddCallback(a => a.SelStart, (o, n) => ScheduleLayout());
			uiHelper.AddCallback(a => a.SelEnd, (o, n) => { EnsureVisible(); ScheduleLayout(); });
			uiHelper.AddCallback(a => a.SelHex, (o, n) => ScheduleLayout());
			uiHelper.AddCallback(Canvas.ActualWidthProperty, canvas, () => ScheduleLayout());
			uiHelper.AddCallback(Canvas.ActualHeightProperty, canvas, () => ScheduleLayout());
			uiHelper.AddCallback(ScrollBar.ValueProperty, xScroll, () => ScheduleLayout());
			uiHelper.AddCallback(ScrollBar.ValueProperty, yScroll, () => ScheduleLayout());

			Show();
		}

		void EnsureVisible()
		{
			var y = GetYFromRow(SelEnd / columns);
			yScroll.Value = Math.Min(y, Math.Max(y + rowHeight - canvas.ActualHeight, yScroll.Value));
		}

		void AddCanvasChild(UIElement child, double x, double y)
		{
			Canvas.SetLeft(child, x);
			Canvas.SetTop(child, y);
			canvas.Children.Add(child);
		}

		void AddCanvasText(string str, double x, double y, Brush foreground = null, Brush background = null)
		{
			var text = new TextBlock { Text = str };
			if (foreground != null)
				text.Foreground = foreground;
			if (background != null)
				text.Background = background;
			AddCanvasChild(text, x, y);
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

		Timer timer;
		void ScheduleLayout()
		{
			if (timer != null)
				return;

			timer = new Timer(100) { AutoReset = false };
			timer.Elapsed += (s, e) => { timer.Dispose(); timer = null; Dispatcher.Invoke(Layout); };
			timer.Start();
		}

		void Layout()
		{
			if (Data == null)
				return;

			columns = Math.Min(maxColumns, Math.Max(minColumns, ((int)((canvas.ActualWidth - xStartSpacing - xEndSpacing) / charWidth) - xPosColumns - xPosGap - xHexGap + xHexSpacing) / (3 + xHexSpacing)));
			rows = (Data.Length + columns - 1) / columns;

			xScroll.ViewportSize = canvas.ActualWidth;
			xScroll.Maximum = xEnd - xScroll.ViewportSize;
			xScroll.SmallChange = charWidth;
			xScroll.LargeChange = xScroll.ViewportSize - xScroll.SmallChange;

			yScroll.ViewportSize = canvas.ActualHeight;
			yScroll.Maximum = yEnd - yScroll.ViewportSize;
			yScroll.SmallChange = rowHeight;
			yScroll.LargeChange = yScroll.ViewportSize - yScroll.SmallChange;

			canvas.Children.Clear();

			var startCtr = Math.Max(0, GetRowFromY(yScroll.Value) * columns);
			var endCtr = Math.Min(Data.Length, GetRowFromY(canvas.ActualHeight + rowHeight + yScroll.Value) * columns);

			for (var ctr = startCtr; ctr < endCtr; ++ctr)
			{
				var selected = ((ctr >= SelStart) && (ctr <= SelEnd)) || ((ctr >= SelEnd) && (ctr <= SelStart));
				Brush foreground = null;
				Brush backgroundHex = null;
				Brush backgroundText = null;
				if (selected)
				{
					foreground = Brushes.White;
					if (SelHex)
					{
						backgroundHex = Brushes.Blue;
						backgroundText = Brushes.Gray;
					}
					else
					{
						backgroundHex = Brushes.Gray;
						backgroundText = Brushes.Blue;
					}
				}

				var y = yLinesStart - yScroll.Value + ctr / columns * rowHeight;
				var column = ctr % columns;
				if (column == 0)
					AddCanvasText(String.Format("{0:x" + xPosColumns.ToString() + "}", ctr), xPosition - xScroll.Value, y);
				var xHex = GetXHexFromColumn(column) - xScroll.Value;
				AddCanvasText(String.Format("{0:x2}", Data[ctr]), xHex, y, foreground, backgroundHex);
				var xText = GetXTextFromColumn(column) - xScroll.Value;
				AddCanvasText(Char.IsControl((char)Data[ctr]) ? "\u2022" : ((char)Data[ctr]).ToString(), xText, y, foreground, backgroundText);
			}
		}

		void MouseHandler(Point mousePos)
		{
			var x = mousePos.X + xScroll.Value;
			var row = GetRowFromY(mousePos.Y + yScroll.Value);
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
			SelEnd = pos;
		}

		void KeyDownHandler(object sender, KeyEventArgs e)
		{
			e.Handled = true;
			switch (e.Key)
			{
				case Key.Tab: SelHex = !SelHex; break;
				case Key.Up: SelEnd -= columns; break;
				case Key.Down: SelEnd += columns; break;
				case Key.Left: --SelEnd; break;
				case Key.Right: ++SelEnd; break;
				case Key.Home:
					if (controlDown)
						SelEnd = 0;
					else
						SelEnd -= SelEnd % columns;
					break;
				case Key.End:
					if (controlDown)
						SelEnd = Data.Length;
					else
						SelEnd += columns - SelEnd % columns - 1;
					break;
				case Key.PageUp:
					if (controlDown)
						SelEnd -= (SelEnd / columns - GetRowFromY(yScroll.Value + rowHeight - 1)) * columns;
					else
						SelEnd -= (long)(canvas.ActualHeight / rowHeight - 1) * columns;
					break;
				case Key.PageDown:
					if (controlDown)
						SelEnd += (GetRowFromY(canvas.ActualHeight + yScroll.Value - rowHeight) - SelEnd / columns) * columns;
					else
						SelEnd += (long)(canvas.ActualHeight / rowHeight - 1) * columns;
					break;
				case Key.A:
					if (controlOnly)
					{
						SelEnd = Data.Length;
						SelStart = 0;
					}
					break;
				default: e.Handled = false; break;
			}
		}

		void ClickHandler(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton != MouseButton.Left)
				return;

			MouseHandler(e.GetPosition(canvas));
			mouseDown = e.ButtonState == MouseButtonState.Pressed;
			if (mouseDown)
				canvas.CaptureMouse();
			else
				canvas.ReleaseMouseCapture();
			e.Handled = true;
		}

		void MoveHandler(object sender, MouseEventArgs e)
		{
			if (!mouseDown)
				return;

			MouseHandler(e.GetPosition(canvas));
			e.Handled = true;
		}

		void FindCallback(object obj)
		{
		}
	}
}
