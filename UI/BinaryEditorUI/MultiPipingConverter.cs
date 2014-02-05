using System;
using System.Windows.Data;

namespace NeoEdit.UI.BinaryEditorUI
{
	class MultiPipingConverter : IMultiValueConverter
	{
		public IMultiValueConverter Converter1 { get; set; }
		public IValueConverter Converter2 { get; set; }

		public object Convert(object[] value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (Converter1 == null)
				return null;

			var convertedValue = Converter1.Convert(value, targetType, parameter, culture);
			convertedValue = Converter2.Convert(convertedValue, targetType, parameter, culture);
			return convertedValue;
		}

		public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
