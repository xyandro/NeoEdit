using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Markup;
using NeoEdit.Records;

namespace NeoEdit.UI.Converters
{
	class PropertyToShownCheck : MarkupExtension, IMultiValueConverter
	{
		static PropertyToShownCheck converter;
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (converter == null)
				converter = new PropertyToShownCheck();
			return converter;
		}

		public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
		{
			var property = (RecordProperty.PropertyName)value[0];
			var properties = value[1] as IEnumerable<RecordProperty.PropertyName>;
			return properties.Contains(property);
		}

		public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}
