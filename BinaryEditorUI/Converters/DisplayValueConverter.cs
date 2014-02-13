using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using NeoEdit.Common;
using NeoEdit.Data;

namespace NeoEdit.BinaryEditorUI.Converters
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
				var encoding = (Coder.Type)value[3];

				var selCount = Math.Min(encoding.PreviewSize(), (selStart == selEnd) ? Int32.MaxValue : selEnd - selStart + 1);
				var str = Coder.BytesToString(data.Data, selStart, selCount, encoding);
				if (encoding.IsStr())
					str = str.Replace("\r", @"\r").Replace("\n", @"\n");
				return str;
			}
			catch { return "Invalid"; }
		}

		public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}
