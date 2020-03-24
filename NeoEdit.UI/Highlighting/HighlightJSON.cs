using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace NeoEdit.UI.Highlighting
{
	class HighlightJSON : Highlight
	{
		static Regex numberRE = new Regex(@"[-+]?[0-9]*\.?[0-9]+([eE][-+]?[0-9]+)?", RegexOptions.Compiled);
		static Brush numberBrush = new SolidColorBrush(Color.FromRgb(181, 206, 168));
		static Regex stringRE = new Regex(@"""([^\\""]|\\.)*""", RegexOptions.Compiled);
		static Brush stringBrush = new SolidColorBrush(Color.FromRgb(214, 157, 133));
		static Regex headerRE = new Regex(@"""([^\\""]|\\.)*"":", RegexOptions.Compiled);
		static Brush headerBrush = new SolidColorBrush(Color.FromRgb(215, 186, 125));
		static Regex commentRE = new Regex("//.*?$|/\\*.*?\\*/", RegexOptions.Compiled);
		static Brush commentBrush = new SolidColorBrush(Color.FromRgb(87, 166, 74));

		static HighlightJSON()
		{
			numberBrush.Freeze();
			stringBrush.Freeze();
			headerBrush.Freeze();
			commentBrush.Freeze();
		}

		public override Dictionary<Regex, Brush> GetDictionary()
		{
			return new Dictionary<Regex, Brush>
			{
				[numberRE] = numberBrush,
				[stringRE] = stringBrush,
				[headerRE] = headerBrush,
				[commentRE] = commentBrush,
			};
		}
	}
}
