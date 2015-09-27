using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Tables
{
	public class CellRanges : ObservableCollectionEx<CellRange>
	{
		public CellRanges() { }
		CellRanges(IEnumerable<CellRange> collection) : base(collection) { }

		public IEnumerable<CellLocation> GetCells(int numRows, int numColumns, bool preserveOrder = false)
		{
			var cells = this.SelectMany(selection => selection.GetCells(numRows, numColumns)).Distinct();
			if (!preserveOrder)
				cells = cells.OrderCells();
			return cells;
		}

		public IEnumerable<int> GetColumns(int numColumns, bool preserveOrder = false)
		{
			var cells = this.SelectMany(selection => selection.GetColumns(numColumns)).Distinct();
			if (!preserveOrder)
				cells = cells.OrderBy(column => column);
			return cells;
		}

		public IEnumerable<int> GetRows(int numRows, bool preserveOrder = false)
		{
			var cells = this.SelectMany(selection => selection.GetRows(numRows)).Distinct();
			if (!preserveOrder)
				cells = cells.OrderBy(row => row);
			return cells;
		}

		public CellRanges Copy()
		{
			return new CellRanges(this);
		}
	}
}
