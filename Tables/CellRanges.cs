using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Tables
{
	public class CellRanges : ObservableCollectionEx<CellRange>
	{
		public void Replace(IEnumerable<CellLocation> cellLocations)
		{
			Replace(cellLocations.Select(cellLocation => new CellRange(cellLocation)));
		}

		public IEnumerable<CellLocation> EnumerateCells(int numRows, int numColumns, bool preserveOrder = false)
		{
			var cells = this.SelectMany(selection => selection.EnumerateCells(numRows, numColumns)).Distinct();
			if (!preserveOrder)
				cells = cells.OrderCells();
			return cells;
		}

		public IEnumerable<int> EnumerateColumns(int numColumns, bool preserveOrder = false)
		{
			var cells = this.SelectMany(selection => selection.EnumerateColumns(numColumns)).Distinct();
			if (!preserveOrder)
				cells = cells.OrderBy(column => column);
			return cells;
		}

		public IEnumerable<int> EnumerateRows(int numRows, bool preserveOrder = false)
		{
			var cells = this.SelectMany(selection => selection.EnumerateRows(numRows)).Distinct();
			if (!preserveOrder)
				cells = cells.OrderBy(row => row);
			return cells;
		}

	}
}
