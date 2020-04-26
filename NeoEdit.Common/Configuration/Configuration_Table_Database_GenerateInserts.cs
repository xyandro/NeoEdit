using System.Collections.Generic;

namespace NeoEdit.Common.Configuration
{
	public class Configuration_Table_Database_GenerateInserts : IConfiguration
	{
		public List<int> Columns { get; set; }
		public int BatchSize { get; set; }
		public string TableName { get; set; }
	}
}
