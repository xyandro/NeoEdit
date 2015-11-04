using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Markup;
using NeoEdit.GUI.Controls.ItemGridControl;

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
			var columns = value[1] as IEnumerable<ItemGridColumn>;
			if ((columns == null) && (value[1] is ItemGridColumn))
				columns = new List<ItemGridColumn> { value[1] as ItemGridColumn };
			return columns.Any(column => column.DepProp == menuitem.Property);
		}

		public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
	}
}
