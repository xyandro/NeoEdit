using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using NeoEdit.Records;

namespace NeoEdit.UI.BrowserUI.Converters
{
	class ColumnMenuItems : MarkupExtension, IMultiValueConverter
	{
		static ColumnMenuItems converter;
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (converter == null)
				converter = new ColumnMenuItems();
			return converter;
		}

		public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
		{
			var properties = value[0] as IEnumerable<RecordProperty.PropertyName>;
			var records = value[1] as IEnumerable<Record>;
			var browser = value[2] as Browser;
			if ((properties == null) || (records == null))
				return null;

			var usedProperties = records.SelectMany(a => a.Properties).Distinct().OrderBy(a => a).ToList();
			var showProperties = properties.ToList();
			showProperties = showProperties.Concat(usedProperties.Where(a => !showProperties.Contains(a))).ToList();
			showProperties = showProperties.Concat(Helpers.GetValues<RecordProperty.PropertyName>().Where(a => !showProperties.Contains(a))).ToList();
			var ret = showProperties.Select(a => new MenuItem
			{
				Header = RecordProperty.Get(a).MenuHeader,
				IsChecked = properties.Contains(a),
				Foreground = usedProperties.Contains(a) ? Brushes.Black : Brushes.DarkGray,
			}).ToList();
			ret.ForEach(a => a.Click += browser.MenuItemColumnClick);
			return ret;
		}

		public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}
