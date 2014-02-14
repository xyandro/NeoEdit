using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using NeoEdit.Records;

namespace NeoEdit.BrowserUI.Converters
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
			var property = (RecordProperty.PropertyName)value[0];
			var sortProperty = (RecordProperty.PropertyName)value[1];
			var sortAscending = (bool)value[2];

			string sort = "";
			if (sortProperty == property)
				sort = sortAscending ? " \u25b5" : " \u25bf";
			return String.Format("{0}{1}", RecordProperty.Get(property).DisplayName, sort);
		}

		public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
