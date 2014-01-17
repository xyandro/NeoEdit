using NeoEdit.Records;
using System;
using System.Globalization;
using System.Windows.Data;

namespace NeoEdit.UI
{
	class IRecordToStringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (value as Record).Name;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}
