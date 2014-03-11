using System;
using System.Globalization;
using System.Windows.Data;

namespace NeoEdit.GUI.ItemGridControl
{
	class StringFormatConverter : IValueConverter
	{
		static StringFormatConverter converter = new StringFormatConverter();
		public static StringFormatConverter Converter { get { return converter; } }

		StringFormatConverter() { }

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if ((value == null) || (!(parameter is string)))
				return value;
			return String.Format("{0:" + (string)parameter + "}", value);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
