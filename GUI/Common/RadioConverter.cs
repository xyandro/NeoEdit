using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace NeoEdit.GUI.Common
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

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value.ToString() == parameter as string;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (targetType == typeof(Boolean))
				return Boolean.Parse(parameter as string);
			return Enum.Parse(targetType, parameter as string);
		}
	}
}
