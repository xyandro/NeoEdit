using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using NeoEdit.Common;

namespace NeoEdit.UI.BinaryEditorUI
{
	class DisplayValueConverter : MarkupExtension, IMultiValueConverter
	{
		static DisplayValueConverter converter;
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (converter == null)
				converter = new DisplayValueConverter();
			return converter;
		}

		public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
		{
			try
			{
				var data = value[0] as BinaryData;
				if (data == null)
					return "Invalid";

				var selStart = (long)value[1];
				var selEnd = (long)value[2];
				if (selStart > selEnd)
					return new Exception();
				var type = Helpers.ParseEnum<Converter.ConverterType>(value[3] as string);

				var selCount = Math.Min(Converter.PreviewSize(type), (selStart == selEnd) ? Int32.MaxValue : selEnd - selStart + 1);
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
