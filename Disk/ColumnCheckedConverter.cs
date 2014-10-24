using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Markup;
using NeoEdit.GUI.ItemGridControl;

namespace NeoEdit.Disk
{
	public class ColumnCheckedConverter : MarkupExtension, IMultiValueConverter
	{
		static ColumnCheckedConverter converter;
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (converter == null)
				converter = new ColumnCheckedConverter();
			return converter;
		}

		public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
		{
			var menuitem = value[0] as ColumnMenuItem;
			var columns = value[1] as ObservableCollection<ItemGridColumn>;
			return columns.Any(column => column.DepProp == menuitem.Property);
		}

		public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
