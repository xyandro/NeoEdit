using System;
using System.Windows;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Models
{
	public class EditSortDialogResult
	{
		public SortScope SortScope { get; set; }
		public int UseRegion { get; set; }
		public SortType SortType { get; set; }
		public bool CaseSensitive { get; set; }
		public bool Ascending { get; set; }
	}
}
