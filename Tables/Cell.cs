using System;

namespace NeoEdit.Tables
{
	public struct Cell : IComparable<Cell>
	{
		public readonly int Row;
		public readonly int Column;

		public Cell(int row, int column)
		{
			Row = row;
			Column = column;
		}

		public int CompareTo(Cell other)
		{
			if (Column < other.Column)
				return -1;
			if (Column > other.Column)
				return 1;
			if (Row < other.Row)
				return -1;
			if (Row > other.Row)
				return 1;
			return 0;
		}

		public bool Equals(Cell cell)
		{
			return (cell.Row == Row) && (cell.Column == Column);
		}

		public bool Equals(int row, int column)
		{
			return (row == Row) && (column == Column);
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Cell))
				return false;
			return Equals((Cell)obj);
		}

		public static bool operator <(Cell left, Cell right)
		{
			return left.CompareTo(right) < 0;
		}

		public static bool operator >(Cell left, Cell right)
		{
			return left.CompareTo(right) > 0;
		}

		public static bool operator <=(Cell left, Cell right)
		{
			return left.CompareTo(right) <= 0;
		}

		public static bool operator >=(Cell left, Cell right)
		{
			return left.CompareTo(right) >= 0;
		}

		public static bool operator ==(Cell left, Cell right)
		{
			return left.CompareTo(right) == 0;
		}

		public static bool operator !=(Cell left, Cell right)
		{
			return left.CompareTo(right) != 0;
		}

		public override int GetHashCode()
		{
			return (Row << 16) | Column;
		}

		public override string ToString()
		{
			return String.Format("Row {0}, Column {1}", Row, Column);
		}
	}
}
