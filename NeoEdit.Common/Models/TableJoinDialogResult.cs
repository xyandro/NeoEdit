using System.Collections.Generic;

namespace NeoEdit.Common.Models
{
	public class TableJoinDialogResult
	{
		public List<int> LeftColumns { get; set; }
		public List<int> RightColumns { get; set; }
		public Table.JoinType JoinType { get; set; }
	}
}
