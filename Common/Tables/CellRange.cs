using System;
using System.Collections.Generic;

namespace NeoEdit.Common.Tables
{
	public class CellRange
	{
		readonly public Cell Start;
		readonly public Cell End;
		readonly public bool Active;
		public int StartRow { get; private set; }
		public int EndRow { get; private set; }
		public int StartColumn { get; private set; }
		public int EndColumn { get; private set; }
		public int MinRow { get; private set; }
		public int MaxRow { get; private set; }
		public int MinColumn { get; private set; }
		public int MaxColumn { get; private set; }

		public int NumRows => MaxRow - MinRow + 1;
		public int NumColumns => MaxColumn - MinColumn + 1;
		public int NumCells => NumRows * NumColumns;

		public CellRange(int startRow = 0, int startColumn = 0, int? endRow = null, int? endColumn = null, bool active = true) : this(new Cell(startRow, startColumn), new Cell(endRow ?? startRow, endColumn ?? startColumn), active) { }

		public CellRange(Cell start, Cell? end = null, bool active = true)
		{
			Start = start;
			End = end ?? start;
			Active = active;
			CalculateBounds();
		}

		public CellRange(CellRange range, Cell? start = null, Cell? end = null, bool? active = null)
		{
			Start = start ?? range.Start;
			End = end ?? range.End;
			Active = active ?? range.Active;
			CalculateBounds();
		}

		void CalculateBounds()
		{
			if (Active)
			{
				StartRow = Start.Row;
				EndRow = End.Row;
				MinRow = Math.Min(StartRow, EndRow);
				MaxRow = Math.Max(StartRow, EndRow);

				StartColumn = Start.Column;
				EndColumn = End.Column;
				MinColumn = Math.Min(StartColumn, EndColumn);
				MaxColumn = Math.Max(StartColumn, EndColumn);
			}
			else
			{
				StartRow = StartColumn = MinRow = MinColumn = -1;
				EndRow = EndColumn = MaxRow = MaxColumn = -2;
			}
		}

		public bool Contains(Cell cell) => (Active) && (cell.Row >= MinRow) && (cell.Row <= MaxRow) && (cell.Column >= MinColumn) && (cell.Column <= MaxColumn);
		public bool Contains(int row, int column) => (Active) && (row >= MinRow) && (row <= MaxRow) && (column >= MinColumn) && (column <= MaxColumn);

		public IEnumerable<Cell> EnumerateCells()
		{
			if (!Active)
				yield break;

			var deltaRow = StartRow > EndRow ? -1 : 1;
			var deltaColumn = StartColumn > EndColumn ? -1 : 1;
			for (var row = StartRow; row != EndRow + deltaRow; row += deltaRow)
				for (var column = StartColumn; column != EndColumn + deltaColumn; column += deltaColumn)
					yield return new Cell(row, column);
		}

		public IEnumerable<int> EnumerateColumns()
		{
			if (!Active)
				yield break;

			var deltaColumn = StartColumn > EndColumn ? -1 : 1;
			for (var column = StartColumn; column != EndColumn + deltaColumn; column += deltaColumn)
				yield return column;
		}

		public IEnumerable<int> EnumerateRows()
		{
			if (!Active)
				yield break;

			var deltaRow = StartRow > EndRow ? -1 : 1;
			for (var row = StartRow; row != EndRow + deltaRow; row += deltaRow)
				yield return row;
		}

		public bool Equals(CellRange range) => (MinColumn.Equals(range.MinColumn)) && (MaxColumn.Equals(range.MaxColumn)) && (MinRow.Equals(range.MinRow)) && (MaxRow.Equals(range.MaxRow)) && (Active.Equals(range.Active));

		public override bool Equals(object obj)
		{
			if (!(obj is CellRange))
				return false;
			return Equals(obj as CellRange);
		}

		public override int GetHashCode() => (MinColumn << 0) + (MaxColumn << 8) + (MinRow << 16) + (MaxRow << 24);

		public override string ToString() => $"Start: {Start}, End: {End}, Active {Active}";
	}
}
