using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace NeoEdit
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
			var supported2 = new bool[chars.Keys.Max() + 1];
			foreach (var pair in chars)
			{
				var fs = new FormattedText(Encoding.UTF32.GetString(BitConverter.GetBytes(pair.Key)), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, Typeface, 14, Brushes.Black);
				supported[pair.Key] = (pair.Key == ' ') || (fs.Width == 8.43);
				supported2[pair.Key] = true;
			}

			var mismatch = Enumerable.Range(0, supported.Length).Where(index => supported[index] != supported2[index]).ToList();
		}

		static unsafe string GetShowSpecialText(string str)
		{
			if ((supported == null) || (ShowSpecialChars) || (str.Length == 0))
				return str;

			var bytes = Encoding.UTF32.GetBytes(str);
			var count = bytes.Length / 4;
			fixed (byte* bytesFixed = bytes)
			{
				var intFixed = (int*)bytesFixed;
				for (var ctr = 0; ctr < count; ++ctr)
					if ((intFixed[ctr] >= supported.Length) || (!supported[intFixed[ctr]]))
						intFixed[ctr] = 0;
			}
			return Encoding.UTF32.GetString(bytes);
		}

		public static FormattedText GetText(string str) => new FormattedText(GetShowSpecialText(str), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, Typeface, FontSize, Brushes.Black);
	}
}
