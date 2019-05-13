using System;
using System.Globalization;
using System.Windows.Data;
using NeoEdit.TextEdit;
using NeoEdit.TextEdit.Transform;

namespace NeoEdit.TextEdit.Converters
{
	class ValidValueConverter : IValueConverter, IMultiValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => Coder.CanEncode(value as string, Helpers.ParseEnum<Coder.CodePage>(parameter as string));
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) => Coder.CanEncode(values[0] as string, (Coder.CodePage)values[1]);
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
	}
}
