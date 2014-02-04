using System;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using System.Windows.Markup;

namespace NeoEdit.UI.BinaryEditorUI
{
	class ValueConverter : MarkupExtension, IMultiValueConverter
	{
		static ValueConverter converter;
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (converter == null)
				converter = new ValueConverter();
			return converter;
		}

		public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
		{
			try
			{
				var data = value[0] as byte[];
				if (data == null)
					throw new Exception();

				var selStart = (long)value[1];
				var selEnd = (long)value[2];
				if (selStart > selEnd)
					return new Exception();
				var type = Helpers.ParseEnum<Converter.ConverterType>(value[3] as string);

				var selCount = (selStart == selEnd) ? 0 : selEnd - selStart + 1;
				return Converter.Convert(type, data, selStart, selCount);
			}
			catch { return "Invalid"; }
		}

		public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}
