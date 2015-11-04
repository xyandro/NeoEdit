using System;
using System.Globalization;
using System.Windows.Data;
using NeoEdit.Common;
using NeoEdit.Common.Transform;

namespace NeoEdit.GUI.Converters
{
	class ValidValueConverter : IValueConverter, IMultiValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => Coder.TryStringToBytes(value as string, Helpers.ParseEnum<Coder.CodePage>(parameter as string)) != null;
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) => Coder.CanFullyEncode(values[0] as string, (Coder.CodePage)values[1]);
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
	}
}
