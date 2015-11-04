using System.Windows;
using NeoEdit.Common;

namespace NeoEdit.GUI.Controls.ItemGridControl
{
	public class ItemGridColumn
	{
		public string Header { get; }
		public DependencyProperty DepProp { get; }
		public object StringFormat { get; set; }
		public HorizontalAlignment HorizontalAlignment { get; set; }
		public bool SortAscending { get; set; }
		public bool NumericStrings { get; set; }

		public ItemGridColumn(DependencyProperty depProp)
		{
			Header = depProp.Name;
			DepProp = depProp;
			var type = depProp.PropertyType;
			HorizontalAlignment = ((!type.IsEnum) && (type.IsIntegerType()) || (type.IsDateType())) ? HorizontalAlignment.Right : HorizontalAlignment.Left;
			StringFormat = type.IsEnum ? null : type.IsIntegerType() ? "n0" : type.IsDateType() ? "yyyy/MM/dd hh:mm:ss tt" : null;
			SortAscending = (!type.IsIntegerType()) && (!type.IsDateType());
			NumericStrings = true;
		}

		public override string ToString() => $"{Header}: {DepProp.Name}";
	}
}
