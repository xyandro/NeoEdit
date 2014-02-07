using System;
using System.Globalization;
using System.Windows.Data;
using NeoEdit.Common;

namespace NeoEdit.BinaryEditorUI.Converters
{
	class ValidValueConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return BinaryData.FromString(Helpers.ParseEnum<BinaryData.EncodingName>(parameter as string), value as string) != null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
