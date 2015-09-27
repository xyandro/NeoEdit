using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace NeoEdit.Tables
{
	static class Misc
	{
		static Misc()
		{
			nullBrush.Freeze();
			headersBrush.Freeze();
			selectedBrush.Freeze();
			linesPen.Freeze();
			activePen.Freeze();
		}

		static internal readonly Brush nullBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
		static internal readonly Brush headersBrush = new SolidColorBrush(Color.FromArgb(255, 192, 192, 192));
		static internal readonly Brush selectedBrush = new SolidColorBrush(Color.FromArgb(255, 128, 128, 255));
		static internal readonly Pen linesPen = new Pen(new SolidColorBrush(Color.FromArgb(32, 0, 0, 0)), 1);
		static internal readonly Pen activePen = new Pen(new SolidColorBrush(Color.FromArgb(255, 0, 0, 0)), 1);

		static internal IOrderedEnumerable<CellLocation> OrderCells(this IEnumerable<CellLocation> cells)
		{
			return cells.OrderBy(cell => cell.Row).ThenBy(cell => cell.Column);
		}

	}
}
