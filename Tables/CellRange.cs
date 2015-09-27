using System;
using System.Collections.Generic;

namespace NeoEdit.Tables
{
	public class CellRange
	{
		int startRow, endRow, startColumn, endColumn, minRow, maxRow, minColumn, maxColumn;

		readonly public CellLocation Start;
		readonly public CellLocation End;
		readonly public bool AllRows;
		readonly public bool AllColumns;

		public CellRange(int row, int column, bool allRows = false, bool allColumns = false) : this(new CellLocation(row, column), allRows, allColumns) { }

		public CellRange(CellLocation location, bool allRows = false, bool allColumns = false)
		{
			Start = End = location;
			AllRows = allRows;
			AllColumns = allColumns;
			CalculateBounds();
		}

		public CellRange(CellRange range, CellLocation start = null, CellLocation end = null, bool? allRows = null, bool? allColumns = null)
		{
			Start = start ?? range.Start;
			End = end ?? range.End;
			AllRows = allRows ?? range.AllRows;
			AllColumns = allColumns ?? range.AllColumns;
			CalculateBounds();
		}

		void CalculateBounds()
		{
			if (AllColumns)
			{
				startRow = minRow = 0;
				endRow = maxRow = int.MaxValue;
			}
			else
			{
				startRow = Start.Row;
				endRow = End.Row;
				minRow = Math.Min(startRow, endRow);
				maxRow = Math.Max(startRow, endRow);
			}
			if (AllRows)
			{
				startColumn = minColumn = 0;
				endColumn = maxColumn = int.MaxValue;
			}
			else
			{
				startColumn = Start.Column;
				endColumn = End.Column;
				minColumn = Math.Min(startColumn, endColumn);
				maxColumn = Math.Max(startColumn, endColumn);
			}
		}

		public bool Contains(CellLocation cellLocation)
		{
			return (cellLocation.Row >= minRow) && (cellLocation.Row <= maxRow) && (cellLocation.Column >= minColumn) && (cellLocation.Column <= maxColumn);
		}

		public bool Contains(int row, int column)
		{
			return (row >= minRow) && (row <= maxRow) && (column >= minColumn) && (column <= maxColumn);
		}

		public IEnumerable<CellLocation> GetCells(int numRows, int numColumns)
		{
			var startRow = Math.Min(numRows - 1, this.startRow);
			var endRow = Math.Min(numRows - 1, this.endRow);
			var deltaRow = startRow > endRow ? -1 : 1;
			endRow += deltaRow;

			var startColumn = Math.Min(numColumns - 1, this.startColumn);
			var endColumn = Math.Min(numColumns - 1, this.endColumn);
			var deltaColumn = startColumn > endColumn ? -1 : 1;
			endColumn += deltaColumn;

			for (var row = startRow; row != endRow; row += deltaRow)
				for (var column = startColumn; column != endColumn; column += deltaColumn)
					yield return new CellLocation(row, column);
		}

		public IEnumerable<int> GetColumns(int numColumns)
		{
			var startColumn = Math.Min(numColumns - 1, this.startColumn);
			var endColumn = Math.Min(numColumns - 1, this.endColumn);
			var deltaColumn = startColumn > endColumn ? -1 : 1;
			endColumn += deltaColumn;

			for (var column = startColumn; column != endColumn; column += deltaColumn)
				yield return column;
		}

		public IEnumerable<int> GetRows(int numRows)
		{
			var startRow = Math.Min(numRows - 1, this.startRow);
			var endRow = Math.Min(numRows - 1, this.endRow);
			var deltaRow = startRow > endRow ? -1 : 1;
			endRow += deltaRow;

			for (var row = startRow; row != endRow; row += deltaRow)
				yield return row;
		}

		public bool Equals(CellRange range)
		{
			return (Start.Equals(range.Start)) && (End.Equals(range.End)) && (AllRows.Equals(range.AllRows)) && (AllColumns.Equals(range.AllColumns));
		}

		public override bool Equals(object obj)
		{
			if (!(obj is CellRange))
				return false;
			return Equals(obj as CellRange);
		}

		public override int GetHashCode()
		{
			return Start.GetHashCode() + End.GetHashCode();
		}

		public override string ToString()
		{
			return String.Format("Start: {0}, End: {1}, AllRows {2}, AllColumns {3}", Start, End, AllRows, AllColumns);
		}
	}
}
