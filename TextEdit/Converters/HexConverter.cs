using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using NeoEdit.TextEdit.Transform;

namespace NeoEdit.TextEdit.Converters
{
	public class HexConverter : IValueConverter
	{
		string lastValue = null;

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var bytes = value as byte[];
			if (bytes == null)
				return null;

			if (lastValue == null)
				lastValue = Coder.BytesToString(bytes, Coder.CodePage.Hex);
			return lastValue;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			lastValue = value as string;
			if (string.IsNullOrEmpty(lastValue))
			{
				lastValue = null;
				return null;
			}

			var bytes = Coder.TryStringToBytes(lastValue, Coder.CodePage.Hex);
			if (bytes == null)
				return DependencyProperty.UnsetValue;
			return bytes;
		}
	}
}
