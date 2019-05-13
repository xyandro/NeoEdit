using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace NeoEdit.Misc
{
	public static class Font
	{
		public delegate void FontSizeChangedDelegate(double newSize);
		public static event FontSizeChangedDelegate FontSizeChanged;
		public static FontFamily FontFamily { get; }
		public static Typeface Typeface { get; }
		static double fontSize;
		public static double CharWidth { get; private set; }
		public static double FontSize
		{
			get { return fontSize; }
			set
			{
				fontSize = value;
				var formattedText = GetText("0123456789 abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ !@#$%^&*()");
				CharWidth = formattedText.Width / "0123456789 abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ !@#$%^&*()".Length;
				FontSizeChanged?.Invoke(fontSize);
			}
		}

		static Font()
		{
			FontFamily = new FontFamily(new Uri("pack://application:,,,/NeoEdit.TextEdit;component/"), "./Resources/#DejaVu Sans Mono");
			Typeface = FontFamily.GetTypefaces().First();
			FontSize = 14;
		}

		public static FormattedText GetText(string str) => new FormattedText(str, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, Typeface, FontSize, Brushes.Black);
	}
}
