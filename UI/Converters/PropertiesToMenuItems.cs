using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Markup;
using NeoEdit.Records;

namespace NeoEdit.UI.Converters
{
	class PropertiesToMenuItems : MarkupExtension, IValueConverter
	{
		static PropertiesToMenuItems converter;
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (converter == null)
				converter = new PropertiesToMenuItems();
			return converter;
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var properties = value as IEnumerable<Property.PropertyType>;
			if (properties == null)
				return null;

			return properties.Concat(Enum.GetValues(typeof(Property.PropertyType)).Cast<Property.PropertyType>().Where(a => !properties.Contains(a)));
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}
