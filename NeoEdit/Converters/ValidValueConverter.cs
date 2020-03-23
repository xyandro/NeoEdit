using System;
using System.Globalization;
using System.Windows.Data;
using NeoEdit.Common;
using NeoEdit.Common.Transform;

namespace NeoEdit.Program.Converters
{
	public class ValidValueConverter : IValueConverter, IMultiValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is string str))
				return false;
			return Coder.CanEncode(str, Helpers.ParseEnum<Coder.CodePage>(parameter as string));
		}

		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(values[0] is string str))
				return false;
			return Coder.CanEncode(str, (Coder.CodePage)values[1]);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
	}
}
