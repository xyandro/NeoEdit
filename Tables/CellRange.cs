﻿using System;
using System.Collections.Generic;

namespace NeoEdit.Tables
{
	public class CellRange
	{
		readonly public CellLocation Start;
		readonly public CellLocation End;
		readonly public bool AllRows;
		readonly public bool AllColumns;
		public int StartRow { get; private set; }
		public int EndRow { get; private set; }
		public int StartColumn { get; private set; }
		public int EndColumn { get; private set; }
		public int MinRow { get; private set; }
		public int MaxRow { get; private set; }
		public int MinColumn { get; private set; }
		public int MaxColumn { get; private set; }

		public int NumRows { get { return MaxRow - MinRow + 1; } }
		public int NumColumns { get { return MaxColumn - MinColumn + 1; } }

		public CellRange(int startRow = 0, int startColumn = 0, int? endRow = null, int? endColumn = null, bool allRows = false, bool allColumns = false) : this(new CellLocation(startRow, startColumn), new CellLocation(endRow ?? startRow, endColumn ?? startColumn), allRows, allColumns) { }

		public CellRange(CellLocation start, CellLocation end = null, bool allRows = false, bool allColumns = false)
		{
			Start = start;
			End = end ?? start;
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
				StartRow = MinRow = 0;
				EndRow = MaxRow = int.MaxValue;
			}
			else
			{
				StartRow = Start.Row;
				EndRow = End.Row;
				MinRow = Math.Min(StartRow, EndRow);
				MaxRow = Math.Max(StartRow, EndRow);
			}
			if (AllRows)
			{
				StartColumn = MinColumn = 0;
				EndColumn = MaxColumn = int.MaxValue;
			}
			else
			{
				StartColumn = Start.Column;
				EndColumn = End.Column;
				MinColumn = Math.Min(StartColumn, EndColumn);
				MaxColumn = Math.Max(StartColumn, EndColumn);
			}
		}

		public bool Contains(CellLocation cellLocation)
		{
			return (cellLocation.Row >= MinRow) && (cellLocation.Row <= MaxRow) && (cellLocation.Column >= MinColumn) && (cellLocation.Column <= MaxColumn);
		}

		public bool Contains(int row, int column)
		{
			return (row >= MinRow) && (row <= MaxRow) && (column >= MinColumn) && (column <= MaxColumn);
		}

		public IEnumerable<CellLocation> EnumerateCells(int numRows, int numColumns)
		{
			var startRow = Math.Min(numRows - 1, StartRow);
			var endRow = Math.Min(numRows - 1, EndRow);
			var deltaRow = startRow > endRow ? -1 : 1;
			endRow += deltaRow;

			var startColumn = Math.Min(numColumns - 1, StartColumn);
			var endColumn = Math.Min(numColumns - 1, EndColumn);
			var deltaColumn = startColumn > endColumn ? -1 : 1;
			endColumn += deltaColumn;

			for (var row = startRow; row != endRow; row += deltaRow)
				for (var column = startColumn; column != endColumn; column += deltaColumn)
					yield return new CellLocation(row, column);
		}

		public IEnumerable<int> EnumerateColumns(int numColumns)
		{
			var startColumn = Math.Min(numColumns - 1, StartColumn);
			var endColumn = Math.Min(numColumns - 1, EndColumn);
			var deltaColumn = startColumn > endColumn ? -1 : 1;
			endColumn += deltaColumn;

			for (var column = startColumn; column != endColumn; column += deltaColumn)
				yield return column;
		}

		public IEnumerable<int> EnumerateRows(int numRows)
		{
			var startRow = Math.Min(numRows - 1, StartRow);
			var endRow = Math.Min(numRows - 1, EndRow);
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
