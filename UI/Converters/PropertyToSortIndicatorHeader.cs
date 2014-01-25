using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using NeoEdit.Records;

namespace NeoEdit.UI.Converters
{
	class PropertyToSortIndicatorHeader : IMultiValueConverter
	{
		RecordProperty.PropertyName property;
		public PropertyToSortIndicatorHeader(RecordProperty.PropertyName _property)
		{
			property = _property;
		}

		public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
		{
			var sortProperty = (RecordProperty.PropertyName)value[0];
			var sortAscending = (bool)value[1];

			string sort = "";
			if (sortProperty == property)
				sort = sortAscending ? " \u25b5" : " \u25bf";
			return String.Format("{0}{1}", RecordProperty.Get(property).DisplayName, sort);
		}

		public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}
