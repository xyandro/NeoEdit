using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using NeoEdit.Common.Transform;
using NeoEdit.HexEdit.Data;

namespace NeoEdit.HexEdit.Converters
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
				var codePage = (Coder.CodePage)value[4];
				if (selStart > selEnd)
					return "Invalid";

				var count = data.Length - selStart;
				if (selStart != selEnd)
					count = Math.Min(count, selEnd - selStart);

				if (Coder.IsStr(codePage))
				{
					count = Math.Min(count, codePage.PreviewSize());
					var bytes1 = data.GetSubset(selStart, count);
					if (!Coder.CanFullyEncode(bytes1, codePage))
						return "Invalid";
					return Coder.BytesToString(bytes1, codePage).Replace("\r", @"\r").Replace("\n", @"\n");
				}
				else
				{
					var coder = (Coder.CodePage)value[4];
					count = Math.Min(count, coder.BytesRequired());
					return Coder.BytesToString(data.GetSubset(selStart, count), coder);
				}
			}
			catch { return "Invalid"; }
		}

		public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
