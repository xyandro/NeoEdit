using System.Collections.Generic;

namespace NeoEdit.Common.Configuration
{
	public class TableDatabaseGenerateDeletesDialogResult : IConfiguration
	{
		public List<int> Where { get; set; }
		public string TableName { get; set; }
	}
}
