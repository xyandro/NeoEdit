using System;
using System.Globalization;
using System.Windows.Data;
using NeoEdit.Records;

namespace NeoEdit.UI
{
	class RecordToStringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			value = (value as Record)[Helpers.ParseEnum<Record.Property>(parameter as string)];
			if (value == null)
				return null;

			if (value.GetType().IsIntegerType())
				return String.Format("{0:n0}", value);
			if (value is DateTime)
				return ((DateTime)value).ToLocalTime();
			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}
