using System;
using System.Globalization;
using System.Windows.Data;
using NeoEdit.Common;
using NeoEdit.Common.Transform;

namespace NeoEdit.GUI.BinaryEditor.Converters
{
	class ValidValueConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Coder.StringToBytes(value as string, Helpers.ParseEnum<Coder.Type>(parameter as string)) != null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
