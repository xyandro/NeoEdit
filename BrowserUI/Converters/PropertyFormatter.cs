using System;
using System.Globalization;
using System.Windows.Data;

namespace NeoEdit.BrowserUI.Converters
{
	class PropertyFormatter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return null;

			if (value is Enum)
				return value.ToString();
			if (value.GetType().IsIntegerType())
				return String.Format("{0:n0}", value);
			if (value is DateTime)
				return ((DateTime)value).ToLocalTime().ToString();
			return value.ToString();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}
