using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace NeoEdit.TextEdit.Highlighting
{
	class HighlightMarkup : Highlight
	{
		const string name = @"[:a-zA-Z\u2070-\u218f\u2c00-\u2fef\u3001-\ud7ff\uf900-\ufdcf\ufdf0-\ufffd][-:a-zA-Z\u2070-\u218f\u2c00-\u2fef\u3001-\ud7ff\uf900-\ufdcf\ufdf0-\ufffd_.0-9\u00b7\u0300-\u036f\u203f-\u2040]*";

		static Regex symbolsRE = new Regex(@"<\??|(\?|/)?>|=", RegexOptions.Compiled);
		static Brush symbolsBrush = new SolidColorBrush(Color.FromRgb(0, 0, 255));
		static Regex attributeRE = new Regex($"(?<=<[^>]*){name}", RegexOptions.Compiled);
		static Brush attributeBrush = new SolidColorBrush(Color.FromRgb(255, 0, 0));
		static Regex tagRE = new Regex($"(?<=<\\??/?){name}", RegexOptions.Compiled);
		static Brush tagBrush = new SolidColorBrush(Color.FromRgb(163, 21, 21));
		static Regex stringRE = new Regex(@"""[^<""]*""|'[^<']*'", RegexOptions.Compiled);
		static Brush stringBrush = new SolidColorBrush(Color.FromRgb(0, 0, 255));
		static Regex commentRE = new Regex(@"<!--.*?-->", RegexOptions.Compiled);
		static Brush commentBrush = new SolidColorBrush(Color.FromRgb(0, 128, 0));

		static HighlightMarkup()
		{
			symbolsBrush.Freeze();
			attributeBrush.Freeze();
			tagBrush.Freeze();
			stringBrush.Freeze();
			commentBrush.Freeze();
		}

		public override Dictionary<Regex, Brush> GetDictionary()
		{
			return new Dictionary<Regex, Brush>
			{
				[symbolsRE] = symbolsBrush,
				[attributeRE] = attributeBrush,
				[tagRE] = tagBrush,
				[stringRE] = stringBrush,
				[commentRE] = commentBrush,
			};
		}
	}
}
