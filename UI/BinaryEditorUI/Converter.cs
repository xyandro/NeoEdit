﻿using System;
using System.Text;

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
			Single,
			Double,
			UTF7,
			UTF8,
			UTF16LE,
			UTF16BE,
			UTF32LE,
			UTF32BE,
			Hex,
			HexRev,
		};

		static byte[] GetBytes(byte[] data, long index, long numBytes, long count, bool littleEndian)
		{
			var ret = new byte[count];
			count = Math.Min(count, data.Length - index);
			if (numBytes != 0)
				count = Math.Min(count, numBytes);
			Array.Copy(data, index, ret, 0, count);
			if (!littleEndian)
				Array.Reverse(ret, 0, (int)count);
			return ret;
		}

		static string GetString(Encoding encoding, byte[] data, long index, long numBytes, long count)
		{
			try
			{
				if (numBytes == 0)
					numBytes = count;
				numBytes = Math.Min(numBytes, data.Length - index);
				var str = encoding.GetString(data, (int)index, (int)numBytes);
				str = str.Replace("\r", @"\r").Replace("\n", @"\n");
				return str;
			}
			catch { return "Failed"; }
		}

		static int GetHexValue(char c)
		{
			if ((c >= '0') && (c <= '9'))
				return c - '0';
			if ((c >= 'A') && (c <= 'F'))
				return c - 'A' + 10;
			throw new Exception("Invalid character");
		}

		static char GetHexChar(int val)
		{
			if ((val >= 0) && (val < 10))
				return (char)('0' + val);
			if ((val >= 10) && (val < 16))
				return (char)('A' + val - 10);
			throw new Exception("Invalid character");
		}

		static byte[] StringToHex(string str, bool reverse)
		{
			str = str.ToUpper().Replace(",", "").Replace(" ", "").Replace("-", "");

			if (str.Length % 2 != 0)
				str = "0" + str;
			var ret = new byte[str.Length / 2];
			for (var ctr = 0; ctr < str.Length; ctr += 2)
				ret[ctr / 2] = (byte)(GetHexValue(str[ctr]) * 16 + GetHexValue(str[ctr + 1]));
			if (reverse)
				Array.Reverse(ret);
			return ret;
		}

		static string HexToString(byte[] data, long index, long numBytes, bool reverse)
		{
			if (numBytes == 0)
				numBytes = data.Length - index;
			numBytes = Math.Min(data.Length - index, numBytes);
			var sb = new StringBuilder((int)(numBytes * 2));
			for (var ctr = 0; ctr < numBytes; ctr++)
			{
				var ch = reverse ? data[index + numBytes - ctr - 1] : data[index + ctr];
				sb.Append(GetHexChar(ch >> 4));
				sb.Append(GetHexChar(ch & 15));
			}
			return sb.ToString();
		}

		static public string Convert(ConverterType type, byte[] data, long index, long numBytes)
		{
			switch (type)
			{
				case ConverterType.UInt8LE: return GetBytes(data, index, numBytes, 1, true)[0].ToString();
				case ConverterType.UInt16LE: return BitConverter.ToUInt16(GetBytes(data, index, numBytes, 2, true), 0).ToString();
				case ConverterType.UInt32LE: return BitConverter.ToUInt32(GetBytes(data, index, numBytes, 4, true), 0).ToString();
				case ConverterType.UInt64LE: return BitConverter.ToUInt64(GetBytes(data, index, numBytes, 8, true), 0).ToString();
				case ConverterType.Int8LE: return ((sbyte)GetBytes(data, index, numBytes, 1, true)[0]).ToString();
				case ConverterType.Int16LE: return BitConverter.ToInt16(GetBytes(data, index, numBytes, 2, true), 0).ToString();
				case ConverterType.Int32LE: return BitConverter.ToInt32(GetBytes(data, index, numBytes, 4, true), 0).ToString();
				case ConverterType.Int64LE: return BitConverter.ToInt64(GetBytes(data, index, numBytes, 8, true), 0).ToString();
				case ConverterType.UInt8BE: return GetBytes(data, index, numBytes, 1, false)[0].ToString();
				case ConverterType.UInt16BE: return BitConverter.ToUInt16(GetBytes(data, index, numBytes, 2, false), 0).ToString();
				case ConverterType.UInt32BE: return BitConverter.ToUInt32(GetBytes(data, index, numBytes, 4, false), 0).ToString();
				case ConverterType.UInt64BE: return BitConverter.ToUInt64(GetBytes(data, index, numBytes, 8, false), 0).ToString();
				case ConverterType.Int8BE: return ((sbyte)GetBytes(data, index, numBytes, 1, false)[0]).ToString();
				case ConverterType.Int16BE: return BitConverter.ToInt16(GetBytes(data, index, numBytes, 2, false), 0).ToString();
				case ConverterType.Int32BE: return BitConverter.ToInt32(GetBytes(data, index, numBytes, 4, false), 0).ToString();
				case ConverterType.Int64BE: return BitConverter.ToInt64(GetBytes(data, index, numBytes, 8, false), 0).ToString();
				case ConverterType.Single: return BitConverter.ToSingle(GetBytes(data, index, numBytes, 4, true), 0).ToString();
				case ConverterType.Double: return BitConverter.ToDouble(GetBytes(data, index, numBytes, 8, true), 0).ToString();
				case ConverterType.UTF7: return GetString(Encoding.UTF7, data, index, numBytes, 100);
				case ConverterType.UTF8: return GetString(Encoding.UTF8, data, index, numBytes, 100);
				case ConverterType.UTF16LE: return GetString(Encoding.Unicode, data, index, numBytes, 200);
				case ConverterType.UTF16BE: return GetString(Encoding.BigEndianUnicode, data, index, numBytes, 200);
				case ConverterType.UTF32LE: return GetString(Encoding.UTF32, data, index, numBytes, 400);
				case ConverterType.UTF32BE: return GetString(new UTF32Encoding(true, false), data, index, numBytes, 400);
				case ConverterType.Hex: return HexToString(data, index, numBytes, false);
				case ConverterType.HexRev: return HexToString(data, index, numBytes, true);
			}
			throw new Exception("Invalid conversion");
		}

		static byte[] Reverse(byte[] data)
		{
			Array.Reverse(data);
			return data;
		}

		static public byte[] Convert(ConverterType type, string value)
		{
			switch (type)
			{
				case ConverterType.UInt8LE: return new byte[] { Byte.Parse(value) };
				case ConverterType.UInt16LE: return BitConverter.GetBytes(UInt16.Parse(value));
				case ConverterType.UInt32LE: return BitConverter.GetBytes(UInt32.Parse(value));
				case ConverterType.UInt64LE: return BitConverter.GetBytes(UInt64.Parse(value));
				case ConverterType.Int8LE: return new byte[] { (byte)SByte.Parse(value) };
				case ConverterType.Int16LE: return BitConverter.GetBytes(Int16.Parse(value));
				case ConverterType.Int32LE: return BitConverter.GetBytes(Int32.Parse(value));
				case ConverterType.Int64LE: return BitConverter.GetBytes(Int64.Parse(value));
				case ConverterType.UInt8BE: return new byte[] { Byte.Parse(value) };
				case ConverterType.UInt16BE: return Reverse(BitConverter.GetBytes(UInt16.Parse(value)));
				case ConverterType.UInt32BE: return Reverse(BitConverter.GetBytes(UInt32.Parse(value)));
				case ConverterType.UInt64BE: return Reverse(BitConverter.GetBytes(UInt64.Parse(value)));
				case ConverterType.Int8BE: return new byte[] { (byte)SByte.Parse(value) };
				case ConverterType.Int16BE: return Reverse(BitConverter.GetBytes(Int16.Parse(value)));
				case ConverterType.Int32BE: return Reverse(BitConverter.GetBytes(Int32.Parse(value)));
				case ConverterType.Int64BE: return Reverse(BitConverter.GetBytes(Int64.Parse(value)));
				case ConverterType.Single: return BitConverter.GetBytes(Single.Parse(value));
				case ConverterType.Double: return BitConverter.GetBytes(Double.Parse(value));
				case ConverterType.UTF7: return Encoding.UTF7.GetBytes(value);
				case ConverterType.UTF8: return Encoding.UTF8.GetBytes(value);
				case ConverterType.UTF16LE: return Encoding.Unicode.GetBytes(value);
				case ConverterType.UTF16BE: return Encoding.BigEndianUnicode.GetBytes(value);
				case ConverterType.UTF32LE: return Encoding.UTF32.GetBytes(value);
				case ConverterType.UTF32BE: return new UTF32Encoding(true, false).GetBytes(value);
				case ConverterType.Hex: return StringToHex(value, false);
				case ConverterType.HexRev: return StringToHex(value, true);
			}
			throw new Exception("Invalid conversion");
		}
	}
}
