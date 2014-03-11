using System;
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

		public ItemGridColumn(DependencyProperty depProp)
		{
			Header = depProp.Name;
			DepProp = depProp;
			HorizontalAlignment = (depProp.PropertyType == typeof(int?)) || (depProp.PropertyType == typeof(long?)) || (depProp.PropertyType == typeof(DateTime?)) ? HorizontalAlignment.Right : HorizontalAlignment.Left;
			StringFormat = depProp.PropertyType == typeof(long?) ? "n0" : depProp.PropertyType == typeof(DateTime?) ? "yyyy/MM/dd HH:mm:ss" : null;
			SortAscending = NumericStrings = true;
		}
	}
}
