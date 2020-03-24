using System.Collections.Generic;

namespace NeoEdit.Common.Models
{
	public class TableEditTableDialogResult
	{
		public List<Table.AggregateData> AggregateData { get; set; }
		public List<Table.SortData> SortData { get; set; }
	}
}
