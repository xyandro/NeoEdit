using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NeoEdit.Program;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Models
{
	public class TableEditTableDialogResult
	{
		public List<Table.AggregateData> AggregateData { get; set; }
		public List<Table.SortData> SortData { get; set; }
	}
}
