using System;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using System.Windows.Markup;

namespace NeoEdit.UI.BinaryEditorUI
{
	static class Converter
	{
		public enum ConverterType
		{
			UInt8LE,
			UInt16LE,
			UInt32LE,
			UInt64LE,
			Int8LE,
			Int16LE,
			Int32LE,
			Int64LE,
			UInt8BE,
			UInt16BE,
			UInt32BE,
			UInt64BE,
			Int8BE,
			Int16BE,
			Int32BE,
			Int64BE,
			Float,
			Double,
			UTF8,
			UTF16LE,
			UTF16BE,
			UTF32LE,
			UTF32BE,
		};

		static byte[] GetBytes(byte[] data, long index, long selSize, int count, bool littleEndian)
		{
			var ret = new byte[count];
			count = (int)Math.Min(count, data.Length - index);
			if (selSize != 0)
				count = (int)Math.Min(count, selSize);
			Array.Copy(data, index, ret, 0, count);
			if (!littleEndian)
				Array.Reverse(ret, 0, count);
			return ret;
		}

		static string GetString(Encoding encoding, byte[] data, long index, long selSize)
		{
			try
			{
				var count = Math.Min(100, data.Length - index);
				if (selSize != 0)
					count = Math.Min(count, selSize);
				var str = encoding.GetString(data, (int)index, (int)count);
				str = str.Replace("\r", @"\r").Replace("\n", @"\n");
				return str;
			}
			catch { return "Failed"; }
		}

		static public string Convert(byte[] data, long index, long selSize, ConverterType type)
		{
			switch (type)
			{
				case ConverterType.UInt8LE: return GetBytes(data, index, selSize, 1, true)[0].ToString();
				case ConverterType.UInt16LE: return BitConverter.ToUInt16(GetBytes(data, index, selSize, 2, true), 0).ToString();
				case ConverterType.UInt32LE: return BitConverter.ToUInt32(GetBytes(data, index, selSize, 4, true), 0).ToString();
				case ConverterType.UInt64LE: return BitConverter.ToUInt64(GetBytes(data, index, selSize, 8, true), 0).ToString();
				case ConverterType.Int8LE: return ((sbyte)GetBytes(data, index, selSize, 1, true)[0]).ToString();
				case ConverterType.Int16LE: return BitConverter.ToInt16(GetBytes(data, index, selSize, 2, true), 0).ToString();
				case ConverterType.Int32LE: return BitConverter.ToInt32(GetBytes(data, index, selSize, 4, true), 0).ToString();
				case ConverterType.Int64LE: return BitConverter.ToInt64(GetBytes(data, index, selSize, 8, true), 0).ToString();
				case ConverterType.UInt8BE: return GetBytes(data, index, selSize, 1, false)[0].ToString();
				case ConverterType.UInt16BE: return BitConverter.ToUInt16(GetBytes(data, index, selSize, 2, false), 0).ToString();
				case ConverterType.UInt32BE: return BitConverter.ToUInt32(GetBytes(data, index, selSize, 4, false), 0).ToString();
				case ConverterType.UInt64BE: return BitConverter.ToUInt64(GetBytes(data, index, selSize, 8, false), 0).ToString();
				case ConverterType.Int8BE: return ((sbyte)GetBytes(data, index, selSize, 1, false)[0]).ToString();
				case ConverterType.Int16BE: return BitConverter.ToInt16(GetBytes(data, index, selSize, 2, false), 0).ToString();
				case ConverterType.Int32BE: return BitConverter.ToInt32(GetBytes(data, index, selSize, 4, false), 0).ToString();
				case ConverterType.Int64BE: return BitConverter.ToInt64(GetBytes(data, index, selSize, 8, false), 0).ToString();
				case ConverterType.Float: return BitConverter.ToSingle(GetBytes(data, index, selSize, 4, false), 0).ToString();
				case ConverterType.Double: return BitConverter.ToDouble(GetBytes(data, index, selSize, 8, false), 0).ToString();
				case ConverterType.UTF8: return GetString(Encoding.UTF8, data, index, selSize);
				case ConverterType.UTF16LE: return GetString(Encoding.Unicode, data, index, selSize);
				case ConverterType.UTF16BE: return GetString(Encoding.BigEndianUnicode, data, index, selSize);
				case ConverterType.UTF32LE: return GetString(Encoding.UTF32, data, index, selSize);
				case ConverterType.UTF32BE: return GetString(new UTF32Encoding(true, false), data, index, selSize);
			}
			throw new Exception("Invalid conversion");
		}

		static public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}
