using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace NeoEdit.Program
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

		static bool showSpecialChars = false;
		public static bool ShowSpecialChars
		{
			get { return showSpecialChars; }
			set
			{
				showSpecialChars = value;
				ShowSpecialCharsChanged?.Invoke(null, new EventArgs());
			}
		}

		public static event EventHandler ShowSpecialCharsChanged;

		readonly static bool[] supported;

		static Font()
		{
			FontFamily = new FontFamily(new Uri("pack://application:,,,/NeoEdit;component/"), "./Resources/#DejaVu Sans Mono");
			Typeface = FontFamily.GetTypefaces().First();
			FontSize = 14;

			Typeface.TryGetGlyphTypeface(out var glyphTypeface);
			var chars = glyphTypeface.CharacterToGlyphMap;
			supported = new bool[chars.Keys.Max() + 1];
			foreach (var pair in chars)
			{
				var fs = new FormattedText(Encoding.UTF32.GetString(BitConverter.GetBytes(pair.Key)), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, Typeface, 14, Brushes.White);
				supported[pair.Key] = (pair.Key == ' ') || (fs.Width == 8.43);
			}
		}

		public static string RemoveSpecialChars(string str)
		{
			if ((supported == null) || (string.IsNullOrEmpty(str)))
				return str;

			var result = new StringBuilder();
			for (var ctr = 0; ctr < str.Length; ++ctr)
			{
				var codePoint = char.ConvertToUtf32(str, ctr);
				var isHighSurrogate = char.IsHighSurrogate(str[ctr]);
				if (isHighSurrogate)
					++ctr;

				if ((!isHighSurrogate) && (codePoint < supported.Length) && (supported[codePoint]))
					result.Append(char.ConvertFromUtf32(codePoint));
				else
				{
					// Invalid char
					result.Append("￿");
					if (isHighSurrogate)
						result.Append("￿");
				}
			}
			return result.ToString();
		}

		public static FormattedText GetText(string str)
		{
			if (!ShowSpecialChars)
				str = RemoveSpecialChars(str);
			return new FormattedText(str, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, Typeface, FontSize, Brushes.White);
		}
	}
}
