using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace NeoEdit.TextEdit
{
	static class Misc
	{
		static Misc()
		{
			selectionBrush.Freeze();
			searchBrush.Freeze();
			regionBrush.Freeze();
			visibleCursorBrush.Freeze();
			diffMajorBrush.Freeze();
			diffMinorBrush.Freeze();
			cursorBrush.Freeze();
			cursorPen.Freeze();
		}

		static internal readonly Brush selectionBrush = new SolidColorBrush(Color.FromArgb(128, 58, 143, 205)); //9cc7e6
		static internal readonly Brush searchBrush = new SolidColorBrush(Color.FromArgb(128, 197, 205, 173)); //e2e6d6
		static internal readonly Brush regionBrush = new SolidColorBrush(Color.FromArgb(64, 0, 128, 0));
		static internal readonly Brush visibleCursorBrush = new SolidColorBrush(Color.FromArgb(20, 0, 0, 0));
		static internal readonly Brush diffMajorBrush = new SolidColorBrush(Color.FromArgb(192, 239, 203, 5));
		static internal readonly Brush diffMinorBrush = new SolidColorBrush(Color.FromArgb(64, 239, 203, 5));
		static internal readonly Brush cursorBrush = new SolidColorBrush(Color.FromArgb(10, 0, 0, 0));
		static internal readonly Pen cursorPen = new Pen(new SolidColorBrush(Color.FromArgb(20, 0, 0, 0)), 1);

		static internal string GetCharsFromRegexString(string regex)
		{
			if (string.IsNullOrEmpty(regex))
				return "";

			var sb = new StringBuilder();
			var matches = Regex.Matches(Regex.Unescape(regex), "(.)-(.)|(.)", RegexOptions.Singleline);
			foreach (Match match in matches)
			{
				if (match.Groups[1].Success)
				{
					var v0 = match.Groups[1].Value[0];
					var v1 = match.Groups[2].Value[0];
					var start = (char)Math.Min(v0, v1);
					var end = (char)Math.Max(v0, v1);
					for (var c = start; c <= end; ++c)
						sb.Append(c);
				}
				else if (match.Groups[3].Success)
					sb.Append(match.Groups[3].Value[0]);
			}

			return sb.ToString();
		}
	}
}
