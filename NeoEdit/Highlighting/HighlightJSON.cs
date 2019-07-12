using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace NeoEdit.Program.Highlighting
{
	class HighlightJSON : Highlight
	{
		static Regex numberRE = new Regex(@"[-+]?[0-9]*\.?[0-9]+([eE][-+]?[0-9]+)?", RegexOptions.Compiled);
		static Brush numberBrush = new SolidColorBrush(Color.FromRgb(0, 0, 255));
		static Regex stringRE = new Regex(@"""([^\\""]|\\.)*""", RegexOptions.Compiled);
		static Brush stringBrush = new SolidColorBrush(Color.FromRgb(163, 21, 21));
		static Regex headerRE = new Regex(@"""([^\\""]|\\.)*"":", RegexOptions.Compiled);
		static Brush headerBrush = new SolidColorBrush(Color.FromRgb(46, 117, 182));

		static HighlightJSON()
		{
			numberBrush.Freeze();
			stringBrush.Freeze();
			headerBrush.Freeze();
		}

		public override Dictionary<Regex, Brush> GetDictionary()
		{
			return new Dictionary<Regex, Brush>
			{
				[numberRE] = numberBrush,
				[stringRE] = stringBrush,
				[headerRE] = headerBrush,
			};
		}
	}
}
