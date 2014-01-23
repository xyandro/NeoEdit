using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Markup;
using NeoEdit.Records;

namespace NeoEdit.UI.Converters
{
	class RecordsToMenuItems : MarkupExtension, IValueConverter
	{
		static RecordsToMenuItems converter;
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (converter == null)
				converter = new RecordsToMenuItems();
			return converter;
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var records = value as IEnumerable<Record>;
			if (records == null) 
				return null;

			return records.SelectMany(a => a.Properties).Distinct().OrderBy(a => (int)a);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}
