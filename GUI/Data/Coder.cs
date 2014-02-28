using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NeoEdit.GUI.Data
{
	public static class Coder
	{
		public enum Type
		{
			None,
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
			Base64,
			Hex,
			HexRev,
		};

		public static bool IsStr(this Type type)
		{
			switch (type)
			{
				case Type.UTF7:
				case Type.UTF8:
				case Type.UTF16LE:
				case Type.UTF16BE:
				case Type.UTF32LE:
				case Type.UTF32BE:
					return true;
				default:
					return false;
			}
		}

		public static int PreviewSize(this Type type)
		{
			switch (type)
			{
				case Type.UTF16LE: return 200;
				case Type.UTF16BE: return 200;
				case Type.UTF32LE: return 400;
				case Type.UTF32BE: return 400;
				default: return 100;
			}
		}

		public static int BytesRequired(this Type type)
		{
			switch (type)
			{
				case Type.UInt8LE: return 1;
				case Type.UInt16LE: return 2;
				case Type.UInt32LE: return 4;
				case Type.UInt64LE: return 8;
				case Type.Int8LE: return 1;
				case Type.Int16LE: return 2;
				case Type.Int32LE: return 4;
				case Type.Int64LE: return 8;
				case Type.UInt8BE: return 1;
				case Type.UInt16BE: return 2;
				case Type.UInt32BE: return 4;
				case Type.UInt64BE: return 8;
				case Type.Int8BE: return 1;
				case Type.Int16BE: return 2;
				case Type.Int32BE: return 4;
				case Type.Int64BE: return 8;
				case Type.Single: return 4;
				case Type.Double: return 8;
				case Type.UTF7:
				case Type.UTF8:
				case Type.UTF16LE:
				case Type.UTF16BE:
				case Type.UTF32LE:
				case Type.UTF32BE:
				case Type.Base64:
				case Type.Hex:
				case Type.HexRev:
					return -1;
			}
			throw new Exception("Invalid type");
		}

		static byte[] Resize(byte[] data, long count, bool littleEndian)
		{
			var ret = new byte[count];
			count = Math.Min(count, data.Length);
			Array.Copy(data, 0, ret, 0, count);
			if (!littleEndian)
				Array.Reverse(ret, 0, (int)count);
			return ret;
		}

		static char GetHexChar(int val)
		{
			if ((val >= 0) && (val < 10))
				return (char)('0' + val);
			return (char)('A' + val - 10);
		}

		static string ToHexString(byte[] data, bool reverse)
		{
			var sb = new StringBuilder(data.Length * 2);
			for (var ctr = 0; ctr < data.Length; ctr++)
			{
				var ch = reverse ? data[data.Length - ctr - 1] : data[ctr];
				sb.Append(GetHexChar(ch >> 4));
				sb.Append(GetHexChar(ch & 15));
			}
			return sb.ToString();
		}

		static Encoding GetEncoding(Type type)
		{
			switch (type)
			{
				case Type.UTF7: return Encoding.UTF7;
				case Type.UTF8: return Encoding.UTF8;
				case Type.UTF16LE: return Encoding.Unicode;
				case Type.UTF16BE: return Encoding.BigEndianUnicode;
				case Type.UTF32LE: return Encoding.UTF32;
				case Type.UTF32BE: return new UTF32Encoding(true, true);
				default: return null;
			}
		}

		public static string BytesToString(byte[] data, Type type)
		{
			switch (type)
			{
				case Type.UInt8LE: return Resize(data, 1, true)[0].ToString();
				case Type.UInt16LE: return BitConverter.ToUInt16(Resize(data, 2, true), 0).ToString();
				case Type.UInt32LE: return BitConverter.ToUInt32(Resize(data, 4, true), 0).ToString();
				case Type.UInt64LE: return BitConverter.ToUInt64(Resize(data, 8, true), 0).ToString();
				case Type.Int8LE: return ((sbyte)Resize(data, 1, true)[0]).ToString();
				case Type.Int16LE: return BitConverter.ToInt16(Resize(data, 2, true), 0).ToString();
				case Type.Int32LE: return BitConverter.ToInt32(Resize(data, 4, true), 0).ToString();
				case Type.Int64LE: return BitConverter.ToInt64(Resize(data, 8, true), 0).ToString();
				case Type.UInt8BE: return Resize(data, 1, false)[0].ToString();
				case Type.UInt16BE: return BitConverter.ToUInt16(Resize(data, 2, false), 0).ToString();
				case Type.UInt32BE: return BitConverter.ToUInt32(Resize(data, 4, false), 0).ToString();
				case Type.UInt64BE: return BitConverter.ToUInt64(Resize(data, 8, false), 0).ToString();
				case Type.Int8BE: return ((sbyte)Resize(data, 1, false)[0]).ToString();
				case Type.Int16BE: return BitConverter.ToInt16(Resize(data, 2, false), 0).ToString();
				case Type.Int32BE: return BitConverter.ToInt32(Resize(data, 4, false), 0).ToString();
				case Type.Int64BE: return BitConverter.ToInt64(Resize(data, 8, false), 0).ToString();
				case Type.Single: return BitConverter.ToSingle(Resize(data, 4, true), 0).ToString();
				case Type.Double: return BitConverter.ToDouble(Resize(data, 8, true), 0).ToString();
				case Type.UTF7:
				case Type.UTF8:
				case Type.UTF16LE:
				case Type.UTF16BE:
				case Type.UTF32LE:
				case Type.UTF32BE:
					return GetEncoding(type).GetString(data);
				case Type.Base64: return Convert.ToBase64String(data);
				case Type.Hex:
				case Type.HexRev:
					return ToHexString(data, type == Type.HexRev);
			}
			throw new Exception("Invalid conversion");
		}

		delegate bool TryParseHandler<T>(string value, out T result);
		static byte[] NumToBytes<T>(string str, TryParseHandler<T> tryParse, Func<T, byte[]> converter, bool reverse)
		{
			T value;
			if (!tryParse(str, out value))
				return null;
			var data = converter(value);
			if (reverse)
				Array.Reverse(data);
			return data;
		}

		static int GetHexValue(char c)
		{
			if ((c >= '0') && (c <= '9'))
				return c - '0';
			return c - 'A' + 10;
		}

		static byte[] StringToHex(string str, bool reverse)
		{
			str = str.ToUpper().Replace(",", "").Replace(" ", "").Replace("-", "");
			for (var ctr = 0; ctr < str.Length; ctr++)
				if (((str[ctr] < '0') || (str[ctr] > '9')) && ((str[ctr] < 'A') || (str[ctr] > 'F')))
					return null;

			if (str.Length % 2 != 0)
				str = "0" + str;
			var ret = new byte[str.Length / 2];
			for (var ctr = 0; ctr < str.Length; ctr += 2)
				ret[ctr / 2] = (byte)(GetHexValue(str[ctr]) * 16 + GetHexValue(str[ctr + 1]));
			if (reverse)
				Array.Reverse(ret);
			return ret;
		}

		static Regex base64Quick = new Regex("^[\ufeff0-9a-zA-Z+/\\s=]*$");
		static Regex base64Correct = new Regex(@"^\ufeff?((\s*[0-9a-zA-Z+/]){4})*((\s*[0-9a-zA-Z+/]){2}\s*=\s*=|(\s*[0-9a-zA-Z+/]){3}\s*=)?\s*$");
		public static byte[] StringToBytes(string value, Type type)
		{
			if (value == null)
				value = "";

			try
			{
				switch (type)
				{
					case Type.UInt8LE: return NumToBytes<byte>(value, Byte.TryParse, v => new byte[] { v }, false);
					case Type.UInt16LE: return NumToBytes<UInt16>(value, UInt16.TryParse, v => BitConverter.GetBytes(v), false);
					case Type.UInt32LE: return NumToBytes<UInt32>(value, UInt32.TryParse, v => BitConverter.GetBytes(v), false);
					case Type.UInt64LE: return NumToBytes<UInt64>(value, UInt64.TryParse, v => BitConverter.GetBytes(v), false);
					case Type.Int8LE: return NumToBytes<sbyte>(value, SByte.TryParse, v => new byte[] { (byte)v }, false);
					case Type.Int16LE: return NumToBytes<Int16>(value, Int16.TryParse, v => BitConverter.GetBytes(v), false);
					case Type.Int32LE: return NumToBytes<Int32>(value, Int32.TryParse, v => BitConverter.GetBytes(v), false);
					case Type.Int64LE: return NumToBytes<Int64>(value, Int64.TryParse, v => BitConverter.GetBytes(v), false);
					case Type.UInt8BE: return NumToBytes<byte>(value, Byte.TryParse, v => new byte[] { v }, true);
					case Type.UInt16BE: return NumToBytes<UInt16>(value, UInt16.TryParse, v => BitConverter.GetBytes(v), true);
					case Type.UInt32BE: return NumToBytes<UInt32>(value, UInt32.TryParse, v => BitConverter.GetBytes(v), true);
					case Type.UInt64BE: return NumToBytes<UInt64>(value, UInt64.TryParse, v => BitConverter.GetBytes(v), true);
					case Type.Int8BE: return NumToBytes<sbyte>(value, SByte.TryParse, v => new byte[] { (byte)v }, true);
					case Type.Int16BE: return NumToBytes<Int16>(value, Int16.TryParse, v => BitConverter.GetBytes(v), true);
					case Type.Int32BE: return NumToBytes<Int32>(value, Int32.TryParse, v => BitConverter.GetBytes(v), true);
					case Type.Int64BE: return NumToBytes<Int64>(value, Int64.TryParse, v => BitConverter.GetBytes(v), true);
					case Type.Single: return NumToBytes<Single>(value, Single.TryParse, v => BitConverter.GetBytes(v), false);
					case Type.Double: return NumToBytes<Double>(value, Double.TryParse, v => BitConverter.GetBytes(v), false);
					case Type.UTF7:
					case Type.UTF8:
					case Type.UTF16LE:
					case Type.UTF16BE:
					case Type.UTF32LE:
					case Type.UTF32BE:
						return GetEncoding(type).GetBytes(value);
					case Type.Base64:
						// Quick match to throw out most invalid stuff; checks only that the string contains the proper characters
						if (!base64Quick.IsMatch(value))
							return null;
						// More expensive one for true correctness
						if (!base64Correct.IsMatch(value))
							return null;
						return Convert.FromBase64String(value.TrimStart('\ufeff'));
					case Type.Hex: return StringToHex(value, false);
					case Type.HexRev: return StringToHex(value, true);
				}
			}
			catch
			{
				// Don't let it get here.  .NET exceptions are expensive.
				return null;
			}
			throw new Exception("Invalid conversion");
		}

		public static Type GuessEncoding(byte[] data)
		{
			var preambles = Helpers.GetValues<Type>().Where(a => a.IsStr()).Select(a => new { type = a, preamble = Coder.StringToBytes("\ufeff", a) }).OrderByDescending(a => a.preamble.Length).ToDictionary(a => a.type, a => a.preamble);

			foreach (var preamble in preambles)
			{
				var match = true;
				if (data.Length < preamble.Value.Length)
					match = false;
				if (match)
					for (var ctr = 0; ctr < preamble.Value.Length; ctr++)
						if (data[ctr] != preamble.Value[ctr])
							match = false;
				if (match)
					return preamble.Key;
			}

			if (data.Length >= 4)
			{
				if (BitConverter.ToUInt32(data, 0) <= 0xffff)
					return Type.UTF32LE;
				if (BitConverter.ToUInt32(data.Take(4).Reverse().ToArray(), 0) <= 0xffff)
					return Type.UTF32BE;
			}

			var zeroIndex = Array.IndexOf(data, (byte)0);
			if (zeroIndex == -1)
				return Type.UTF8;
			else if ((zeroIndex & 1) == 1)
				return Type.UTF16LE;
			else
				return Type.UTF16BE;
		}
	}
}
