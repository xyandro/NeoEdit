using System;

namespace NeoEdit.Tables
{
	public class CellLocation
	{
		public int Row { get; private set; }
		public int Column { get; private set; }

		public CellLocation(int row, int column)
		{
			Row = row;
			Column = column;
		}

		public override string ToString()
		{
			return String.Format("Row {0}, Column {1}", Row, Column);
		}
	}
}
