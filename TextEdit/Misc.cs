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
			cursorBrush.Freeze();
			cursorPen.Freeze();
		}

		static internal readonly Brush selectionBrush = new SolidColorBrush(Color.FromArgb(128, 58, 143, 205)); //9cc7e6
		static internal readonly Brush searchBrush = new SolidColorBrush(Color.FromArgb(128, 197, 205, 173)); //e2e6d6
		static internal readonly Brush regionBrush = new SolidColorBrush(Color.FromArgb(64, 0, 128, 0));
		static internal readonly Brush visibleCursorBrush = new SolidColorBrush(Color.FromArgb(20, 0, 0, 0));
		static internal readonly Brush cursorBrush = new SolidColorBrush(Color.FromArgb(10, 0, 0, 0));
		static internal readonly Pen cursorPen = new Pen(new SolidColorBrush(Color.FromArgb(20, 0, 0, 0)), 1);
	}
}
