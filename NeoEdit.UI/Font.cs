using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using NeoEdit.Common;

namespace NeoEdit.UI
{
	public static class Font
	{
		public static FontFamily FontFamily { get; }
		public static Typeface Typeface { get; }
		public static double CharWidth { get; private set; }
		public static double FontSize { get; private set; }

		static readonly bool[] supported;

		static Font()
		{
			FontFamily = new FontFamily(new Uri("pack://application:,,,/NeoEdit.UI;component/"), "./Resources/#DejaVu Sans Mono");
			Typeface = FontFamily.GetTypefaces().First();

			Typeface.TryGetGlyphTypeface(out var glyphTypeface);
			var chars = glyphTypeface.CharacterToGlyphMap;
			supported = new bool[chars.Keys.Max() + 1];
			foreach (var pair in chars)
			{
				var fs = new FormattedText(Encoding.UTF32.GetString(BitConverter.GetBytes(pair.Key)), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, Typeface, 14, Brushes.White, 1);
				supported[pair.Key] = (pair.Key == ' ') || (fs.Width == 8.43);
			}
		}

		public static void Reset()
		{
			FontSize = Settings.FontSize;
			var formattedText = GetText("0123456789 abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ !@#$%^&*()");
			CharWidth = formattedText.Width / "0123456789 abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ !@#$%^&*()".Length;
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
			if (!Settings.ShowSpecialChars)
				str = RemoveSpecialChars(str);
			return new FormattedText(str, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, Typeface, FontSize, Brushes.White, 1);
		}
	}
}
