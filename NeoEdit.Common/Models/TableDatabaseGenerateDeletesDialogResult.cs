using System.Collections.Generic;

namespace NeoEdit.Common.Models
{
	public class TableDatabaseGenerateDeletesDialogResult
	{
		public List<int> Where { get; set; }
		public string TableName { get; set; }
	}
}
