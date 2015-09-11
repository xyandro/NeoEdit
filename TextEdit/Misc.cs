using System;
using System.Collections.Generic;
using System.Linq;
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
			if (String.IsNullOrEmpty(regex))
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

		static internal IEnumerable<TSource> Resize<TSource>(this IEnumerable<TSource> source, int count, TSource expandWith)
		{
			return source.Take(count).Expand(count, expandWith);
		}

		static internal IEnumerable<TSource> Expand<TSource>(this IEnumerable<TSource> source, int count, TSource expandWith)
		{
			foreach (var item in source)
			{
				yield return item;
				--count;
			}
			for (; count > 0; --count)
				yield return expandWith;
		}

		static internal IEnumerable<string> SplitByLine(this IEnumerable<string> source)
		{
			var lineBreakChars = new char[] { '\r', '\n' };
			foreach (var item in source)
			{
				var pos = 0;
				while (pos < item.Length)
				{
					var index = item.IndexOfAny(lineBreakChars, pos);
					if (index == -1)
						index = item.Length;
					yield return item.Substring(pos, index - pos);
					if ((index + 1 < item.Length) && (item[index] == '\r') && (item[index + 1] == '\n'))
						++index;
					pos = index + 1;
				}
			}
		}

		static internal IEnumerable<string> SplitTCSV(this string source, char splitChar)
		{
			var pos = 0;
			while (true)
			{
				var result = "";
				if ((pos < source.Length) && (source[pos] == '"'))
				{
					var quoteIndex = pos + 1;
					while (true)
					{
						quoteIndex = source.IndexOf('"', quoteIndex);
						if (quoteIndex == -1)
						{
							result += source.Substring(pos + 1).Replace(@"""""", @"""");
							pos = source.Length;
							break;
						}

						if ((quoteIndex + 1 < source.Length) && (source[quoteIndex + 1] == '"'))
						{
							quoteIndex += 2;
							continue;
						}

						result += source.Substring(pos + 1, quoteIndex - pos - 1).Replace(@"""""", @"""");
						pos = quoteIndex + 1;
						break;
					}
				}

				var splitIndex = source.IndexOf(splitChar, pos);
				var end = splitIndex == -1;
				if (end)
					splitIndex = source.Length;
				result += source.Substring(pos, splitIndex - pos);
				yield return result;

				if (end)
					break;
				pos = splitIndex + 1;
			}
		}
	}
}
