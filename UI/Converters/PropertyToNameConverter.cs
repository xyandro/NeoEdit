using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using NeoEdit.Records;

namespace NeoEdit.UI.Converters
{
	class PropertyToNameConverter : MarkupExtension, IMultiValueConverter
	{
		static PropertyToNameConverter converter;
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (converter == null)
				converter = new PropertyToNameConverter();
			return converter;
		}

		public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
		{
			var property = (Property.PropertyType)value[0];
			var sortProperty = (Property.PropertyType)value[1];
			var sortAscending = (bool)value[2];

			string sort = "";
			if (sortProperty == property)
				sort = sortAscending ? " \u25bd" : " \u25b3";
			return String.Format("{0}{1}", Property.Get(property).DisplayName, sort);
		}

		public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}
