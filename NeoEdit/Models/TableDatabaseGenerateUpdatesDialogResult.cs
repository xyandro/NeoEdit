using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.Program;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Models
{
	public class TableDatabaseGenerateUpdatesDialogResult
	{
		public List<int> Update { get; set; }
		public List<int> Where { get; set; }
		public string TableName { get; set; }
	}
}
