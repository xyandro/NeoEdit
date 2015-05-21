using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Controls.ItemGridControl;

namespace NeoEdit.Disk
{
	public class ColumnsConverter : MarkupExtension, IValueConverter, IMultiValueConverter
	{
		static ColumnsConverter converter;
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (converter == null)
				converter = new ColumnsConverter();
			return converter;
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == DependencyProperty.UnsetValue)
				return null;
			else if (value == null)
				value = UIHelper<DiskItem>.GetProperties().ToList();
			else if (value is IEnumerable<ItemGridColumn>)
				value = (value as IEnumerable<ItemGridColumn>).Select(a => a.DepProp).ToList();
			else
				throw new Exception("Invalid type");

			return (value as IEnumerable<DependencyProperty>).Select(prop => new ColumnMenuItem(prop)).ToList();
		}

		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			return Convert(values.FirstOrDefault(), targetType, parameter, culture);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
