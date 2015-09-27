using System;

namespace NeoEdit.Tables
{
	public class CellLocation
	{
		public readonly int Row;
		public readonly int Column;

		public CellLocation(int row, int column)
		{
			Row = row;
			Column = column;
		}

		public bool Equals(CellLocation cell)
		{
			return (cell.Row == Row) && (cell.Column == Column);
		}

		public bool Equals(int row, int column)
		{
			return (row == Row) && (column == Column);
		}

		public override bool Equals(object obj)
		{
			if (!(obj is CellLocation))
				return false;
			return Equals(obj as CellLocation);
		}

		public override int GetHashCode()
		{
			return (Row << 16) + Column;
		}

		public override string ToString()
		{
			return String.Format("Row {0}, Column {1}", Row, Column);
		}
	}
}
