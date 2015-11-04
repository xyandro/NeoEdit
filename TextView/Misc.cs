using System.Windows.Media;

namespace NeoEdit.TextView
{
	static class Misc
	{
		static Misc() { selectionBrush.Freeze(); }

		static internal readonly Brush selectionBrush = new SolidColorBrush(Color.FromArgb(128, 58, 143, 205)); //9cc7e6
	}
}
