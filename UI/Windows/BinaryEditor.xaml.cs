using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace NeoEdit.UI.Windows
{
	public partial class BinaryEditor : Window
	{
		[DepProp]
		public byte[] Data { get { return uiHelper.GetPropValue<byte[]>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public FontFamily CustomFontFamily { get { return uiHelper.GetPropValue<FontFamily>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public double CustomFontSize { get { return uiHelper.GetPropValue<double>(); } set { uiHelper.SetPropValue(value); } }

		readonly double charWidth;
		const int minColumns = 4;
		const int maxColumns = Int32.MaxValue;

		int Columns, Rows;

		// X spacing
		const double xStartSpacing = 10;
		const int xPosColumns = 8;
		const int xPosGap = 2;
		const int xHexSpacing = 1;
		const int xHexGap = 2;
		const double xEndSpacing = xStartSpacing;

		double xStart { get { return 0; } }
		double xPosition { get { return xStart + xStartSpacing; } }
		double xHexView { get { return xPosition + charWidth * (xPosColumns + xPosGap); } }
		double xTextView { get { return xHexView + charWidth * (Columns * (2 + xHexSpacing) - xHexSpacing + xHexGap); } }
		double xEnd { get { return xTextView + Columns * charWidth + xEndSpacing; } }

		// Y spacing
		const double yStartSpacing = 10;
		readonly double rowHeight;
		const double yEndSpacing = yStartSpacing;

		double yStart { get { return 0; } }
		double yLinesStart { get { return yStart + yStartSpacing; } }
		double yLinesEnd { get { return yLinesStart + Rows * rowHeight; } }
		double yEnd { get { return yLinesEnd + yEndSpacing; } }

		readonly UIHelper<BinaryEditor> uiHelper;
		public BinaryEditor(byte[] data)
		{
			uiHelper = new UIHelper<BinaryEditor>(this);
			InitializeComponent();

			Data = data;

			CustomFontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./UI/Resources/#Anonymous Pro");
			CustomFontSize = 14;
			rowHeight = CustomFontSize;

			var example = "0123456789 abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ !@#$%^&*()";
			var formattedText = new FormattedText(example, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, CustomFontFamily.GetTypefaces().First(), CustomFontSize, Brushes.Black);
			charWidth = formattedText.Width / example.Length;

			uiHelper.AddCallback(a => a.Data, (o, n) => Layout());
			uiHelper.AddCallback(Canvas.ActualWidthProperty, canvas, () => Layout());
			uiHelper.AddCallback(Canvas.ActualHeightProperty, canvas, () => Layout());
			uiHelper.AddCallback(ScrollBar.ValueProperty, xScroll, () => Layout());
			uiHelper.AddCallback(ScrollBar.ValueProperty, yScroll, () => Layout());

			Show();
		}

		void AddCanvasChild(string str, double x, double y)
		{
			var text = new TextBlock { Text = str };
			Canvas.SetLeft(text, x);
			Canvas.SetTop(text, y);
			canvas.Children.Add(text);
		}

		void Layout()
		{
			if (Data == null)
				return;

			Columns = Math.Min(maxColumns, Math.Max(minColumns, ((int)((canvas.ActualWidth - xStartSpacing - xEndSpacing) / charWidth) - xPosColumns - xPosGap - xHexGap + xHexSpacing) / (3 + xHexSpacing)));
			Rows = (Data.Length + Columns - 1) / Columns;

			xScroll.ViewportSize = canvas.ActualWidth;
			xScroll.Maximum = xEnd - xScroll.ViewportSize;
			xScroll.SmallChange = charWidth;
			xScroll.LargeChange = xScroll.ViewportSize - xScroll.SmallChange;

			yScroll.ViewportSize = canvas.ActualHeight;
			yScroll.Maximum = yEnd - yScroll.ViewportSize;
			yScroll.SmallChange = rowHeight;
			yScroll.LargeChange = yScroll.ViewportSize - yScroll.SmallChange;

			canvas.Children.Clear();

			var startCtr = Math.Max(0, (int)((yScroll.Value - yLinesStart) / rowHeight - 1) * Columns);
			var endCtr = Math.Min(Data.Length, (int)((canvas.ActualHeight + yScroll.Value - yLinesStart) / rowHeight + 1) * Columns);

			for (var ctr = startCtr; ctr < endCtr; ctr += Columns)
			{
				var y = yLinesStart - yScroll.Value + ctr / Columns * rowHeight;
				var line = new byte[Math.Min(Columns, Data.Length - ctr)];
				Array.Copy(Data, ctr, line, 0, line.Length);
				AddCanvasChild(String.Format("{0:x" + xPosColumns.ToString() + "}", ctr), xPosition - xScroll.Value, y);
				AddCanvasChild(BitConverter.ToString(line).Replace("-", new string(' ', xHexSpacing)), xHexView - xScroll.Value, y);
				AddCanvasChild(new String(line.Select(a => (char)a).Select(a => Char.IsControl(a) ? '\u2022' : a).ToArray()), xTextView - xScroll.Value, y);

			}
		}
	}
}
