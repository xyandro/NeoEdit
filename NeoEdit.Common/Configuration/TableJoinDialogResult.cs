using System.Collections.Generic;

namespace NeoEdit.Common.Configuration
{
	public class TableJoinDialogResult : IConfiguration
	{
		public List<int> LeftColumns { get; set; }
		public List<int> RightColumns { get; set; }
		public Table.JoinType JoinType { get; set; }
	}
}
