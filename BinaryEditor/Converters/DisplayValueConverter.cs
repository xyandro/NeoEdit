using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using NeoEdit.BinaryEditor.Data;
using NeoEdit.Common.Transform;

namespace NeoEdit.BinaryEditor.Converters
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
					return "Invalid";

				var count = data.Length - selStart;
				if (selStart != selEnd)
					count = Math.Min(count, selEnd - selStart);

				if (value[4] is Coder.Type)
				{
					var coder = (Coder.Type)value[4];
					count = Math.Min(count, coder.BytesRequired());
					return Coder.BytesToString(data.GetSubset(selStart, count), coder);
				}

				if (value[4] is StrCoder.CodePage)
				{
					var codePage = (StrCoder.CodePage)value[4];
					count = Math.Min(count, codePage.PreviewSize());
					var bytes1 = data.GetSubset(selStart, count);
					var result = StrCoder.BytesToString(bytes1, codePage);

					// Convert it back to see if it actually worked
					var bytes2 = StrCoder.StringToBytes(result, codePage);
					if (bytes1.Length != bytes2.Length)
						return "Invalid";
					for (var ctr = 0; ctr < bytes1.Length; ++ctr)
						if (bytes1[ctr] != bytes2[ctr])
							return "Invalid";

					return result.Replace("\r", @"\r").Replace("\n", @"\n");
				}

				return "Invalid input format";
			}
			catch { return "Invalid"; }
		}

		public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
