using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.Program;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Models
{
	public class TableJoinDialogResult
	{
		public List<int> LeftColumns { get; set; }
		public List<int> RightColumns { get; set; }
		public Table.JoinType JoinType { get; set; }
	}
}
