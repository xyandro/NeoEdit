using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeoEdit.Common.Transform
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
			AutoUnicode,
			AutoFromBOM,
			UTF8,
			UTF16LE,
			UTF16BE,
			UTF32LE,
			UTF32BE,
			Default,
			Base64,
			Hex,
		};

		public static bool IsStr(this Type type)
		{
			switch (type)
			{
				case Type.UTF8:
				case Type.UTF16LE:
				case Type.UTF16BE:
				case Type.UTF32LE:
				case Type.UTF32BE:
				case Type.Default:
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
				case Type.UTF8:
				case Type.UTF16LE:
				case Type.UTF16BE:
				case Type.UTF32LE:
				case Type.UTF32BE:
				case Type.Default:
				case Type.Base64:
				case Type.Hex:
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

		static unsafe string ToHexString(byte[] bytes)
		{
			var hexValue = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };
			var output = new char[bytes.Length * 2];
			fixed (byte* bytesFixed = bytes)
			fixed (char* outputFixed = output)
			{
				var len = bytesFixed + bytes.Length;
				char* outputPtr = outputFixed;
				for (var bytesPtr = bytesFixed; bytesPtr < len; ++bytesPtr)
				{
					*outputPtr++ = hexValue[*bytesPtr >> 4];
					*outputPtr++ = hexValue[*bytesPtr & 15];
				}
			}
			return new String(output);
		}

		static unsafe byte[] FromHexString(string input)
		{
			if ((input.Length % 2) != 0)
				input = '0' + input;
			var bytes = new byte[input.Length >> 1];
			fixed (char* inputFixed = input)
			fixed (byte* bytesFixed = bytes)
			{
				char* inputPtr = inputFixed;
				var len = bytesFixed + bytes.Length;
				for (var bytesPtr = bytesFixed; bytesPtr < len; ++bytesPtr)
				{
					var c = *inputPtr++;
					if ((c >= '0') && (c <= '9'))
						*bytesPtr = (byte)((c - '0') << 4);
					else if ((c >= 'a') && (c <= 'f'))
						*bytesPtr = (byte)((c - 'a' + 10) << 4);
					else if ((c >= 'A') && (c <= 'F'))
						*bytesPtr = (byte)((c - 'A' + 10) << 4);
					else
						throw new Exception("Invalid string");

					c = *inputPtr++;
					if ((c >= '0') && (c <= '9'))
						*bytesPtr += (byte)(c - '0');
					else if ((c >= 'a') && (c <= 'f'))
						*bytesPtr += (byte)(c - 'a' + 10);
					else if ((c >= 'A') && (c <= 'F'))
						*bytesPtr += (byte)(c - 'A' + 10);
					else
						throw new Exception("Invalid string");
				}
			}
			return bytes;
		}

		static Encoding GetEncoding(Type type)
		{
			switch (type)
			{
				case Type.UTF8: return Encoding.UTF8;
				case Type.UTF16LE: return Encoding.Unicode;
				case Type.UTF16BE: return Encoding.BigEndianUnicode;
				case Type.UTF32LE: return Encoding.UTF32;
				case Type.UTF32BE: return new UTF32Encoding(true, true);
				case Type.Default: return Encoding.Default;
				default: return null;
			}
		}

		static byte[] GetPreamble(Type type)
		{
			return GetEncoding(type).GetPreamble();
		}

		public static string BytesToString(byte[] data, Type type)
		{
			if (type == Type.AutoUnicode)
				type = GuessUnicodeEncoding(data);
			else if (type == Type.AutoFromBOM)
				type = EncodingFromBOM(data);

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
				case Type.UTF8:
				case Type.UTF16LE:
				case Type.UTF16BE:
				case Type.UTF32LE:
				case Type.UTF32BE:
				case Type.Default:
					return GetEncoding(type).GetString(data);
				case Type.Base64: return Convert.ToBase64String(data).TrimEnd('=');
				case Type.Hex: return ToHexString(data);
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

		static byte[] GetBase64Bytes(string value)
		{
			var base64 = new StringBuilder();
			bool padding = false;
			for (var ctr = 0; ctr < value.Length; ++ctr)
			{
				if ((ctr == 0) && (value[ctr] == '\ufeff'))
					continue; // Skip BOM
				else if (Char.IsWhiteSpace(value[ctr]))
					continue; // Skip whitespace
				else if (value[ctr] == '=')
					padding = true;
				else if (padding)
					return null; // No more chars allowed after padding starts
				else if ((Char.IsLetterOrDigit(value[ctr])) || (value[ctr] == '+') || (value[ctr] == '/'))
					base64.Append(value[ctr]);
				else
					return null; // Invalid char
			}

			var pad = 3 - ((base64.Length + 3) % 4);
			if ((pad < 0) || (pad > 2))
				return null; // Invalid; 0, 1, 2 are only options

			base64.Append('=', pad);

			return Convert.FromBase64String(base64.ToString());
		}

		public static byte[] TryStringToBytes(string value, Type type)
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
					case Type.UTF8:
					case Type.UTF16LE:
					case Type.UTF16BE:
					case Type.UTF32LE:
					case Type.UTF32BE:
					case Type.Default:
						return GetEncoding(type).GetBytes(value);
					case Type.Base64:
						return GetBase64Bytes(value);
					case Type.Hex: return FromHexString(value);
				}
			}
			catch
			{
				// Don't let it get here.  .NET exceptions are expensive.
				return null;
			}
			throw new Exception("Invalid conversion");
		}

		public static byte[] StringToBytes(string value, Type type)
		{
			var result = TryStringToBytes(value, type);
			if (result == null)
				throw new Exception("Invalid conversion");
			return result;
		}

		public static Type EncodingFromBOM(byte[] data)
		{
			var preambles = Helpers.GetValues<Type>().Where(a => a.IsStr()).Select(a => new { type = a, preamble = Coder.GetPreamble(a) }).OrderByDescending(a => a.preamble.Length).ToDictionary(a => a.type, a => a.preamble);

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

			// Shouldn't get here since Default's BOM is empty and should have returned earlier
			return Type.Default;
		}

		public static Type GuessUnicodeEncoding(byte[] data)
		{
			var encoding = EncodingFromBOM(data);
			if (encoding != Type.Default)
				return encoding;

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

		public static Dictionary<string, Coder.Type> GetEncodingTypes()
		{
			return new Dictionary<string, Type>
			{
				{ "UTF8", Coder.Type.UTF8 },
				{ "UTF16 (Little Endian)", Coder.Type.UTF16LE },
				{ "UTF16 (Big Endian)", Coder.Type.UTF16BE },
				{ "UTF32 (Little Endian)", Coder.Type.UTF32LE },
				{ "UTF32 (Big Endian)", Coder.Type.UTF32BE },
				{ "Default", Coder.Type.Default },
				{ "Base64", Coder.Type.Base64 },
				{ "Hex", Coder.Type.Hex },
			};
		}
	}
}
