using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace NeoEdit.GUI.Converters
{
	public class RadioConverter : MarkupExtension, IValueConverter
	{
		static RadioConverter converter;
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (converter == null)
				converter = new RadioConverter();
			return converter;
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value.ToString() == parameter as string;

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (targetType == typeof(Boolean))
				return Boolean.Parse(parameter as string);
			if ((value is bool) && (!(bool)value))
				return DependencyProperty.UnsetValue;
			return Enum.Parse(targetType, parameter as string);
		}
	}
}
