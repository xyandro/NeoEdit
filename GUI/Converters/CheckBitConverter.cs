using System;
using System.Globalization;
using System.Windows.Data;

namespace NeoEdit.GUI.Converters
{
	public class CheckBitConverter : IValueConverter
	{
		int previousValue;

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if ((!(value is Enum)) || (!(parameter is Enum)) || (value.GetType() != parameter.GetType()))
				return Binding.DoNothing;

			previousValue = System.Convert.ToInt32(value);
			return (previousValue & System.Convert.ToInt32(parameter)) != 0;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if ((!(value is bool)) || (!(parameter is Enum)))
				return Binding.DoNothing;

			var paramValue = System.Convert.ToInt32(parameter);
			return Enum.ToObject(targetType, previousValue & ~paramValue | ((bool)value ? paramValue : 0));
		}
	}
}
