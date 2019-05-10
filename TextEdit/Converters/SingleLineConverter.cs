using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace NeoEdit.TextEdit.Converters
{
	public class SingleLineConverter : MarkupExtension, IValueConverter
	{
		static SingleLineConverter converter;
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (converter == null)
				converter = new SingleLineConverter();
			return converter;
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var str = (value ?? "").ToString().Replace("\r", "\\r").Replace("\n", "\\n");
			// WPF imposes an automatic word wrap at 9600
			return str.Substring(0, Math.Min(str.Length, 9600));
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }

	}
}
