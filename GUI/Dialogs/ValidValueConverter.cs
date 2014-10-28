using System;
using System.Globalization;
using System.Windows.Data;
using NeoEdit.Common;
using NeoEdit.Common.Transform;

namespace NeoEdit.GUI.Dialogs
{
	class ValidValueConverter : IValueConverter, IMultiValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Coder.TryStringToBytes(value as string, Helpers.ParseEnum<Coder.Type>(parameter as string)) != null;
		}

		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			return StrCoder.CanFullyEncode(values[0] as string, (StrCoder.CodePage)values[1]);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
