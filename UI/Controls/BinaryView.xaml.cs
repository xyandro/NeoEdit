using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NeoEdit.UI.Controls
{
	public partial class BinaryView : Canvas
	{
		[DepProp]
		public byte[] Data { get { return uiHelper.GetPropValue<byte[]>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int Columns { get { return uiHelper.GetPropValue<int>(); } private set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public double ScrollWidth { get { return uiHelper.GetPropValue<double>(); } set { uiHelper.SetPropValue(value); } }

		const double fontSize = 14;
		const int minColumns = 4;
		const int maxColumns = Int32.MaxValue;

		int Rows;

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
		const double rowHeight = fontSize;
		const double yEndSpacing = yStartSpacing;

		double yStart { get { return 0; } }
		double yLinesStart { get { return yStart + yStartSpacing; } }
		double yLinesEnd { get { return yLinesStart + Rows * rowHeight; } }
		double yEnd { get { return yLinesEnd + yEndSpacing; } }

		readonly UIHelper<BinaryView> uiHelper;
		readonly double charWidth;
		readonly Typeface typeface;
		public BinaryView()
		{
			uiHelper = new UIHelper<BinaryView>(this);
			InitializeComponent();

			var fontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./UI/Resources/#Anonymous Pro");
			typeface = fontFamily.GetTypefaces().First();

			var example = "0123456789 abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ !@#$%^&*()";
			var formattedText = new FormattedText(example, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
			charWidth = formattedText.Width / example.Length;

			Columns = minColumns;

			uiHelper.AddCallback(a => a.Data, (o, n) => CalculateDimensions());
			uiHelper.AddCallback(a => a.ScrollWidth, (o, n) => CalculateDimensions());
		}

		void CalculateDimensions()
		{
			if (Data == null)
				return;

			Columns = Math.Min(maxColumns, Math.Max(minColumns, ((int)((ScrollWidth - xStartSpacing - xEndSpacing) / charWidth) - xPosColumns - xPosGap - xHexGap + xHexSpacing) / (3 + xHexSpacing)));
			Rows = (Data.Length + Columns - 1) / Columns;

			Width = xEnd;
			Height = yEnd;
			InvalidateVisual();
		}

		void DrawText(DrawingContext dc, string str, double x, double y)
		{
			var formattedText = new FormattedText(str, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
			dc.DrawText(formattedText, new Point(x, y));
		}

		protected override void OnRender(DrawingContext dc)
		{
			if (Data == null)
				return;

			for (var ctr = 0; ctr < Data.Length; ctr += Columns)
			{
				var line = Data.Skip(ctr).Take(Columns).ToArray();
				var y = yLinesStart + ctr / Columns * fontSize;

				DrawText(dc, String.Format("{0:x" + xPosColumns.ToString() + "}", ctr), xPosition, y);
				DrawText(dc, BitConverter.ToString(line).Replace("-", new string(' ', xHexSpacing)), xHexView, y);
				DrawText(dc, new String(line.Select(a => (char)a).Select(a => Char.IsControl(a) ? '\u2022' : a).ToArray()), xTextView, y);
			}
		}
	}
}
