using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace NeoEdit.Highlighting
{
	class HighlightColumns : Highlight
	{
		static Regex singleRE = new Regex("│");
		static Brush singleBrush = new SolidColorBrush(Color.FromRgb(128, 128, 255));
		static Regex doubleRE = new Regex("║");
		static Brush doubleBrush = new SolidColorBrush(Color.FromRgb(0, 0, 255));

		static HighlightColumns()
		{
			singleBrush.Freeze();
			doubleBrush.Freeze();
		}

		public override Dictionary<Regex, Brush> GetDictionary()
		{
			return new Dictionary<Regex, Brush>
			{
				[singleRE] = singleBrush,
				[doubleRE] = doubleBrush,
			};
		}
	}
}
