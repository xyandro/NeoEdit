using System.Windows.Media;

namespace NeoEdit.BinaryEdit
{
	static class Misc
	{
		static Misc()
		{
			selectionActiveBrush.Freeze();
			selectionInactiveBrush.Freeze();
		}

		static internal readonly Brush selectionActiveBrush = new SolidColorBrush(Color.FromArgb(128, 58, 143, 205)); //9cc7e6
		static internal readonly Brush selectionInactiveBrush = new SolidColorBrush(Color.FromArgb(128, 197, 205, 173)); //e2e6d6
	}
}
