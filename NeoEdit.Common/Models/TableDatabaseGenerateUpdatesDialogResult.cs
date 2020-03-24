using System.Collections.Generic;

namespace NeoEdit.Common.Models
{
	public class TableDatabaseGenerateUpdatesDialogResult
	{
		public List<int> Update { get; set; }
		public List<int> Where { get; set; }
		public string TableName { get; set; }
	}
}
