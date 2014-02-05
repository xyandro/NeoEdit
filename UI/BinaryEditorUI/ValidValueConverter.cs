using System;
using System.Globalization;
using System.Windows.Data;

namespace NeoEdit.UI.BinaryEditorUI
{
	class ValidValueConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Converter.Convert(Helpers.ParseEnum<Converter.ConverterType>(parameter as string), value as string) != null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
