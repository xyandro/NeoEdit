using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace NeoEdit.GUI.Converters
{
	public class NonEmptyConverter : MarkupExtension, IValueConverter
	{
		public override object ProvideValue(IServiceProvider serviceProvider) => this;

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is string str)
				return !string.IsNullOrWhiteSpace(str);
			return false;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
