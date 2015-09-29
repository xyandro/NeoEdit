using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Tables
{
	public class CellRanges : ObservableCollectionEx<CellRange>
	{
		public CellRanges() { }
		public CellRanges(IEnumerable<CellRange> collection) : base(collection) { }

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

		public CellRanges Copy()
		{
			return new CellRanges(this);
		}

		public int TotalNumRows { get { return this.Sum(group => group.NumRows); } }
		public int TotalColumnCount { get { return this.Sum(group => group.NumColumns); } }

		public CellRanges SimplifyToRows(bool after = false)
		{
			return new CellRanges(this.GroupBy(selection => selection.MinRow).Select(group => new { minRow = group.Key, maxRow = group.Max(selection => selection.MaxRow) }).Select(obj => new CellRange(startRow: after ? obj.maxRow + 1 : obj.minRow, endRow: after ? obj.maxRow * 2 - obj.minRow + 1 : obj.maxRow, allRows: true)).OrderBy(range => range.MinRow));
		}

		public CellRanges SimplifyToColumns(bool after = false)
		{
			return new CellRanges(this.GroupBy(selection => selection.MinColumn).Select(group => new { minColumn = group.Key, maxColumn = group.Max(selection => selection.MaxColumn) }).Select(obj => new CellRange(startColumn: after ? obj.maxColumn + 1 : obj.minColumn, endColumn: after ? obj.maxColumn * 2 - obj.minColumn + 1 : obj.maxColumn, allColumns: true)).OrderBy(range => range.MinColumn));
		}

		public CellRanges ToOffsetRows()
		{
			var result = new CellRanges();
			var offset = 0;
			foreach (var positionGroup in this)
			{
				result.Add(new CellRange(startRow: positionGroup.MinRow + offset, endRow: positionGroup.MinRow + positionGroup.NumRows - 1 + offset, allRows: true));
				offset += positionGroup.NumRows;
			}
			return result;
		}

		public CellRanges ToOffsetColumns()
		{
			var result = new CellRanges();
			var offset = 0;
			foreach (var positionGroup in this)
			{
				result.Add(new CellRange(startColumn: positionGroup.MinColumn + offset, endColumn: positionGroup.MinColumn + positionGroup.NumColumns - 1 + offset, allColumns: true));
				offset += positionGroup.NumColumns;
			}
			return result;
		}

		public List<int> GetDeleteRows()
		{
			return this.SelectMany(positionGroup => Enumerable.Range(positionGroup.MinRow, positionGroup.NumRows)).ToList();
		}

		public List<int> GetDeleteColumns()
		{
			return this.SelectMany(positionGroup => Enumerable.Range(positionGroup.MinColumn, positionGroup.NumColumns)).ToList();
		}

		public IEnumerable<int> GetInsertRows()
		{
			return this.SelectMany(positionGroup => Enumerable.Repeat(positionGroup.MinRow, positionGroup.NumRows)).Select((val, index) => val + index);
		}

		public IEnumerable<int> GetInsertColumns()
		{
			return this.SelectMany(positionGroup => Enumerable.Repeat(positionGroup.MinColumn, positionGroup.NumColumns)).Select((val, index) => val + index);
		}

		public CellRanges InsertToDeleteRows()
		{
			var result = new CellRanges();
			var offset = 0;
			foreach (var range in this)
			{
				result.Add(new CellRange(startRow: range.MinRow + offset, endRow: range.MaxRow + offset, allRows: true));
				offset += range.NumRows;
			}
			return result;
		}

		public CellRanges InsertToDeleteColumns()
		{
			var result = new CellRanges();
			var offset = 0;
			foreach (var range in this)
			{
				result.Add(new CellRange(startColumn: range.MinColumn + offset, endColumn: range.MaxColumn + offset, allColumns: true));
				offset += range.NumColumns;
			}
			return result;
		}

		public CellRanges DeleteToInsertRows()
		{
			var result = new CellRanges();
			var offset = 0;
			foreach (var range in this)
			{
				result.Add(new CellRange(startRow: range.MinRow - offset, endRow: range.MaxRow - offset, allRows: true));
				offset += range.NumRows;
			}
			return result;
		}

		public CellRanges DeleteToInsertColumns()
		{
			var result = new CellRanges();
			var offset = 0;
			foreach (var range in this)
			{
				result.Add(new CellRange(startColumn: range.MinColumn - offset, endColumn: range.MaxColumn - offset, allColumns: true));
				offset += range.NumColumns;
			}
			return result;
		}
	}
}
