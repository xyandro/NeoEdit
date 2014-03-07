using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using NeoEdit.Records;

namespace NeoEdit.GUI.BrowserUI.Converters
{
	class SortMenuItems : MarkupExtension, IMultiValueConverter
	{
		static SortMenuItems converter;
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (converter == null)
				converter = new SortMenuItems();
			return converter;
		}

		public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
		{
			var properties = value[0] as IEnumerable<RecordProperty.PropertyName>;
			var sortProperty = (RecordProperty.PropertyName)value[1];
			var browser = value[2] as Browser;
			if (properties == null)
				return null;

			var ret = properties.Select(a => new MenuItem
			{
				Header = RecordProperty.Get(a).MenuHeader,
				IsChecked = a == sortProperty,
			}).ToList();
			ret.ForEach(a => a.Click += browser.MenuItemSortClick);
			return ret;
		}

		public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
