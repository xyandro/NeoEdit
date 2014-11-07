using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace NeoEdit.GUI.Common
{
	public static class Font
	{
		public static readonly double charWidth;
		public static readonly double lineHeight;

		static readonly Typeface typeface;
		static readonly double fontSize;

		static Font()
		{
			var fontFamily = new FontFamily(new Uri("pack://application:,,,/NeoEdit.GUI;component/"), "./Resources/#DejaVu Sans Mono");
			typeface = fontFamily.GetTypefaces().First();
			fontSize = 14;
			lineHeight = fontSize;

			var example = "0123456789 abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ !@#$%^&*()";
			var formattedText = new FormattedText(example, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
			charWidth = formattedText.Width / example.Length;
		}

		public static FormattedText GetText(string str, Brush brush = null)
		{
			if (brush == null)
				brush = Brushes.Black;
			return new FormattedText(str, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, brush);
		}
	}
}
