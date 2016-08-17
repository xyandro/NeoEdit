using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.TextEdit.QueryBuilding
{
	class TableSelect : Select
	{
		public string Table { get; }
		public List<string> ColumnNames { get; } = new List<string>();

		public TableSelect(string table, IEnumerable<string> columnNames = null)
		{
			Table = table;
			ColumnNames = columnNames?.ToList() ?? new List<string>();
		}

		public override IEnumerable<string> QueryLines { get { yield return Table; } }

		public override IEnumerable<string> Columns => ColumnNames;
	}
}
