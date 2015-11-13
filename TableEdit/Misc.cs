using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using NeoEdit.Common;
using NeoEdit.Common.Tables;

namespace NeoEdit.TableEdit
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

		static internal IOrderedEnumerable<Cell> OrderCells(this IEnumerable<Cell> cells) => cells.OrderBy(cell => cell.Row).ThenBy(cell => cell.Column);

		static internal void Replace(this ObservableCollectionEx<CellRange> ranges, IEnumerable<Cell> cells) => ranges.Replace(cells.Select(cell => new CellRange(cell)));

		static internal IEnumerable<Cell> EnumerateCells(this ObservableCollectionEx<CellRange> ranges, bool preserveOrder = false)
		{
			var cells = ranges.SelectMany(selection => selection.EnumerateCells()).Distinct();
			if (!preserveOrder)
				cells = cells.OrderCells();
			return cells;
		}

		static internal IEnumerable<int> EnumerateColumns(this ObservableCollectionEx<CellRange> ranges, bool preserveOrder = false)
		{
			var cells = ranges.SelectMany(selection => selection.EnumerateColumns()).Distinct();
			if (!preserveOrder)
				cells = cells.OrderBy();
			return cells;
		}

		static internal IEnumerable<int> EnumerateRows(this ObservableCollectionEx<CellRange> ranges, bool preserveOrder = false)
		{
			var cells = ranges.SelectMany(selection => selection.EnumerateRows()).Distinct();
			if (!preserveOrder)
				cells = cells.OrderBy();
			return cells;
		}
	}
}
