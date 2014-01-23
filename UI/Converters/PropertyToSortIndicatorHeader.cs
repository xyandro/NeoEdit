using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using NeoEdit.Records;

namespace NeoEdit.UI.Converters
{
	class PropertyToSortIndicatorHeader : MarkupExtension, IMultiValueConverter
	{
		static PropertyToSortIndicatorHeader converter;
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (converter == null)
				converter = new PropertyToSortIndicatorHeader();
			return converter;
		}

		public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
		{
			var property = (Property.PropertyType)value[0];
			var sortProperty = (Property.PropertyType)value[1];
			var sortAscending = (bool)value[2];

			string sort = "";
			if (sortProperty == property)
				sort = sortAscending ? " \u25b5" : " \u25bf";
			return String.Format("{0}{1}", Property.Get(property).DisplayName, sort);
		}

		public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}
