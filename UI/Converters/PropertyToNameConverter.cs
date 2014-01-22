using NeoEdit.Records;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace NeoEdit.UI.Converters
{
	class PropertyToNameConverter : MarkupExtension, IValueConverter
	{
		static PropertyToNameConverter converter;
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (converter == null)
				converter = new PropertyToNameConverter();
			return converter;
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Record.DisplayName[(Record.Property)value];
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}
