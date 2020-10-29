using System.Collections.Generic;

namespace NeoEdit.Common.Configuration
{
	public class Configuration_Table_Edit : IConfiguration
	{
		public List<Table.AggregateData> AggregateData { get; set; }
		public List<Table.SortData> SortData { get; set; }
	}
}
