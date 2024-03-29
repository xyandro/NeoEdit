﻿using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace NeoEdit.UI.Highlighting
{
	class HighlightMarkup : Highlight
	{
		const string name = @"[:a-zA-Z\u2070-\u218f\u2c00-\u2fef\u3001-\ud7ff\uf900-\ufdcf\ufdf0-\ufffd][-:a-zA-Z\u2070-\u218f\u2c00-\u2fef\u3001-\ud7ff\uf900-\ufdcf\ufdf0-\ufffd_.0-9\u00b7\u0300-\u036f\u203f-\u2040]*";

		static Regex symbolsRE = new Regex(@"<\??|(\?|/)?>|=", RegexOptions.Compiled);
		static Brush symbolsBrush = new SolidColorBrush(Color.FromRgb(128, 128, 128));
		static Regex attributeRE = new Regex($"(?<=<[^>]*){name}", RegexOptions.Compiled);
		static Brush attributeBrush = new SolidColorBrush(Color.FromRgb(146, 202, 244));
		static Regex tagRE = new Regex($"(?<=<\\??/?){name}", RegexOptions.Compiled);
		static Brush tagBrush = new SolidColorBrush(Color.FromRgb(86, 156, 214));
		static Regex stringRE = new Regex(@"""[^<""]*""|'[^<']*'", RegexOptions.Compiled);
		static Brush stringBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200));
		static Regex commentRE = new Regex(@"<!--.*?-->", RegexOptions.Compiled);
		static Brush commentBrush = new SolidColorBrush(Color.FromRgb(87, 166, 74));

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
