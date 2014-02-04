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

		byte[] GetBytes(byte[] data, long selStart, long selEnd, int count, bool littleEndian)
		{
			var ret = new byte[count];
			count = (int)Math.Min(count, data.Length - selStart);
			if (selStart != selEnd)
				count = (int)Math.Min(count, selEnd - selStart + 1);
			Array.Copy(data, selStart, ret, 0, count);
			if (!littleEndian)
				Array.Reverse(ret, 0, count);
			return ret;
		}

		public string GetString(Encoding encoding, byte[] data, long selStart, long selEnd)
		{
			try
			{
				var count = Math.Min(100, data.Length - selStart);
				if (selStart != selEnd)
					count = Math.Min(count, selEnd - selStart + 1);
				var str = encoding.GetString(data, (int)selStart, (int)count);
				str = str.Replace("\r", @"\r").Replace("\n", @"\n");
				return str;
			}
			catch { return "Failed"; }
		}

		public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
		{
			var data = value[0] as byte[];
			if (data == null)
				return null;

			var tmpSelStart = (long)value[1];
			var tmpSelEnd = (long)value[2];
			var type = value[3] as String;

			var selStart = Math.Min(tmpSelStart, tmpSelEnd);
			var selEnd = Math.Max(tmpSelStart, tmpSelEnd);

			switch (type)
			{
				case "UInt8LE": return GetBytes(data, selStart, selEnd, 1, true)[0].ToString();
				case "UInt16LE": return BitConverter.ToUInt16(GetBytes(data, selStart, selEnd, 2, true), 0).ToString();
				case "UInt32LE": return BitConverter.ToUInt32(GetBytes(data, selStart, selEnd, 4, true), 0).ToString();
				case "UInt64LE": return BitConverter.ToUInt64(GetBytes(data, selStart, selEnd, 8, true), 0).ToString();
				case "Int8LE": return ((sbyte)GetBytes(data, selStart, selEnd, 1, true)[0]).ToString();
				case "Int16LE": return BitConverter.ToInt16(GetBytes(data, selStart, selEnd, 2, true), 0).ToString();
				case "Int32LE": return BitConverter.ToInt32(GetBytes(data, selStart, selEnd, 4, true), 0).ToString();
				case "Int64LE": return BitConverter.ToInt64(GetBytes(data, selStart, selEnd, 8, true), 0).ToString();
				case "UInt8BE": return GetBytes(data, selStart, selEnd, 1, false)[0].ToString();
				case "UInt16BE": return BitConverter.ToUInt16(GetBytes(data, selStart, selEnd, 2, false), 0).ToString();
				case "UInt32BE": return BitConverter.ToUInt32(GetBytes(data, selStart, selEnd, 4, false), 0).ToString();
				case "UInt64BE": return BitConverter.ToUInt64(GetBytes(data, selStart, selEnd, 8, false), 0).ToString();
				case "Int8BE": return ((sbyte)GetBytes(data, selStart, selEnd, 1, false)[0]).ToString();
				case "Int16BE": return BitConverter.ToInt16(GetBytes(data, selStart, selEnd, 2, false), 0).ToString();
				case "Int32BE": return BitConverter.ToInt32(GetBytes(data, selStart, selEnd, 4, false), 0).ToString();
				case "Int64BE": return BitConverter.ToInt64(GetBytes(data, selStart, selEnd, 8, false), 0).ToString();
				case "UTF8": return GetString(Encoding.UTF8, data, selStart, selEnd);
				case "UTF16LE": return GetString(Encoding.Unicode, data, selStart, selEnd);
				case "UTF16BE": return GetString(Encoding.BigEndianUnicode, data, selStart, selEnd);
				case "UTF32LE": return GetString(Encoding.UTF32, data, selStart, selEnd);
				case "UTF32BE": return GetString(new UTF32Encoding(true, false), data, selStart, selEnd);
			}
			return value;
		}

		public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}
