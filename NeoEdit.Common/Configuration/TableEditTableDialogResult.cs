using System.Collections.Generic;

namespace NeoEdit.Common.Configuration
{
	public class TableEditTableDialogResult : IConfiguration
	{
		public List<Table.AggregateData> AggregateData { get; set; }
		public List<Table.SortData> SortData { get; set; }
	}
}
