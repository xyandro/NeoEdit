using System.Collections.Generic;

namespace NeoEdit.Common.Configuration
{
	public class Configuration_Table_Database_GenerateDeletes : IConfiguration
	{
		public List<int> Where { get; set; }
		public string TableName { get; set; }
	}
}
