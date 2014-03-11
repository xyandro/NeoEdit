using System.Windows;

namespace NeoEdit.GUI.ItemGridControl
{
	public class ItemGridColumn
	{
		public string Header { get; set; }
		public DependencyProperty DepProp { get; set; }
		public string StringFormat { get; set; }
		public HorizontalAlignment HorizontalAlignment { get; set; }
		public bool SortAscending { get; set; }
		public bool NumericStrings { get; set; }

		public ItemGridColumn() { NumericStrings = true; }
	}
}
