using System.Collections.Generic;

namespace NeoEdit.Common.Models
{
	public class TableDatabaseGenerateInsertsDialogResult
	{
		public List<int> Columns { get; set; }
		public int BatchSize { get; set; }
		public string TableName { get; set; }
	}
}
