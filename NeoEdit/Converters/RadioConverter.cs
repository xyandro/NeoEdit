using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace NeoEdit.Program.Converters
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
			if ((value is bool) && (!(bool)value))
				return DependencyProperty.UnsetValue;
			if (targetType.IsEnum)
				return Enum.Parse(targetType, parameter as string);
			return System.Convert.ChangeType(parameter, targetType);
		}
	}
}
