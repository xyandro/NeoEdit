using NeoEdit.Records;
using System;
using System.Globalization;
using System.Windows.Data;

namespace NeoEdit.UI
{
	class RecordToStringConverter : IMultiValueConverter
	{
		public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
		{
			var propertyValue = (value[0] as Record)[Helpers.ParseEnum<Record.Property>(value[1] as string)];
			if (propertyValue == null)
				return null;

			if (propertyValue.GetType().IsIntegerType())
				return String.Format("{0:n0}", propertyValue);
			if (propertyValue is DateTime)
				return ((DateTime)propertyValue).ToLocalTime().ToString();
			return propertyValue.ToString();
		}

		public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}
