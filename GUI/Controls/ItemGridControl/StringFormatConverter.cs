using System;
using System.Globalization;
using System.Windows.Data;

namespace NeoEdit.GUI.Controls.ItemGridControl
{
	public delegate string StringFormatDelegate(object obj);

	class StringFormatConverter : IValueConverter
	{
		static StringFormatConverter converter = new StringFormatConverter();
		public static StringFormatConverter Converter => converter;

		StringFormatConverter() { }

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if ((value == null) || (parameter == null))
				return value;
			if (parameter is string)
				return string.Format($"{{0:{parameter}}}", value);
			if (parameter is StringFormatDelegate)
				return (parameter as StringFormatDelegate)(value);
			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
	}
}
