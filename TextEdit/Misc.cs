using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;
using NeoEdit.Common;

namespace NeoEdit.TextEdit
{
	static class Misc
	{
		static Misc()
		{
			selectionBrush.Freeze();
			searchBrush.Freeze();
			regionBrush.Values.ForEach(brush => brush.Freeze());
			visibleCursorBrush.Freeze();
			diffMajorBrush.Freeze();
			diffMinorBrush.Freeze();
			cursorBrush.Freeze();
			cursorPen.Freeze();
		}

		static internal readonly Brush selectionBrush = new SolidColorBrush(Color.FromArgb(128, 58, 143, 205)); //9cc7e6
		static internal readonly Brush searchBrush = new SolidColorBrush(Color.FromArgb(128, 197, 205, 173)); //e2e6d6
		static internal readonly Dictionary<int, Brush> regionBrush = new Dictionary<int, Brush>
		{
			[1] = new SolidColorBrush(Color.FromArgb(64, 0, 64, 0)),
			[2] = new SolidColorBrush(Color.FromArgb(64, 64, 0, 0)),
			[3] = new SolidColorBrush(Color.FromArgb(64, 0, 0, 64)),
			[4] = new SolidColorBrush(Color.FromArgb(64, 64, 64, 0)),
			[5] = new SolidColorBrush(Color.FromArgb(64, 64, 0, 64)),
			[6] = new SolidColorBrush(Color.FromArgb(64, 0, 64, 64)),
			[7] = new SolidColorBrush(Color.FromArgb(64, 0, 128, 0)),
			[8] = new SolidColorBrush(Color.FromArgb(64, 128, 0, 0)),
			[9] = new SolidColorBrush(Color.FromArgb(64, 0, 0, 128)),
		};
		static internal readonly Brush visibleCursorBrush = new SolidColorBrush(Color.FromArgb(20, 0, 0, 0));
		static internal readonly Brush diffMajorBrush = new SolidColorBrush(Color.FromArgb(192, 239, 203, 5));
		static internal readonly Brush diffMinorBrush = new SolidColorBrush(Color.FromArgb(64, 239, 203, 5));
		static internal readonly Brush cursorBrush = new SolidColorBrush(Color.FromArgb(10, 0, 0, 0));
		static internal readonly Pen cursorPen = new Pen(new SolidColorBrush(Color.FromArgb(20, 0, 0, 0)), 1);

		static internal string GetCharsFromCharString(string charString)
		{
			var result = new StringBuilder();
			var range = false;
			foreach (var c in Regex.Unescape(charString))
			{
				if (c == '-')
				{
					range = !range;
					if (range)
						continue;
				}

				if ((range) && (result.Length == 0))
					throw new Exception("Invalid charstring");

				var start = range ? result[result.Length - 1] : c;
				var dir = c > start ? 1 : -1;

				while (true)
				{
					if (range)
						range = false;
					else
						result.Append(start);
					if (start == c)
						break;
					start = (char)(start + dir);
				}
			}
			if (range)
				throw new Exception("Invalid charstring");

			return result.ToString();
		}
	}
}
