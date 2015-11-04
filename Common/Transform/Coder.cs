using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NeoEdit.Common.Transform
{
	public static class Coder
	{
		public enum CodePage
		{
			None = -1000,

			StartNum = -100,
			Int8 = -101,
			Int16LE = -102,
			Int16BE = -103,
			Int32LE = -104,
			Int32BE = -105,
			Int64LE = -106,
			Int64BE = -107,
			UInt8 = -108,
			UInt16LE = -109,
			UInt16BE = -110,
			UInt32LE = -111,
			UInt32BE = -112,
			UInt64LE = -113,
			UInt64BE = -114,
			Single = -115,
			Double = -116,
			EndNum = -119,

			StartString = -50,

			AutoUnicode = -40,
			AutoByBOM = -41,

			StartNonAutoString = -30,

			Hex = -3,
			HexRev = -4,
			Binary = -5,
			Base64 = -6,

			Default = 0,
			ASCII = 437,
			UTF8 = 65001,
			UTF16LE = 1200,
			UTF16BE = 1201,
			UTF32LE = 12000,
			UTF32BE = 12001,
		}

		public static int BytesRequired(this CodePage codePage)
		{
			switch (codePage)
			{
				case CodePage.Int8: return 1;
				case CodePage.Int16LE: return 2;
				case CodePage.Int16BE: return 2;
				case CodePage.Int32LE: return 4;
				case CodePage.Int32BE: return 4;
				case CodePage.Int64LE: return 8;
				case CodePage.Int64BE: return 8;
				case CodePage.UInt8: return 1;
				case CodePage.UInt16LE: return 2;
				case CodePage.UInt16BE: return 2;
				case CodePage.UInt32LE: return 4;
				case CodePage.UInt32BE: return 4;
				case CodePage.UInt64LE: return 8;
				case CodePage.UInt64BE: return 8;
				case CodePage.Single: return 4;
				case CodePage.Double: return 8;
				default: throw new Exception("Invalid type");
			}
		}

		public static string MinValue(this CodePage codePage)
		{
			switch (codePage)
			{
				case CodePage.Int8: return sbyte.MinValue.ToString();
				case CodePage.Int16LE: return Int16.MinValue.ToString();
				case CodePage.Int16BE: return Int16.MinValue.ToString();
				case CodePage.Int32LE: return Int32.MinValue.ToString();
				case CodePage.Int32BE: return Int32.MinValue.ToString();
				case CodePage.Int64LE: return Int64.MinValue.ToString();
				case CodePage.Int64BE: return Int64.MinValue.ToString();
				case CodePage.UInt8: return byte.MinValue.ToString();
				case CodePage.UInt16LE: return UInt16.MinValue.ToString();
				case CodePage.UInt16BE: return UInt16.MinValue.ToString();
				case CodePage.UInt32LE: return UInt32.MinValue.ToString();
				case CodePage.UInt32BE: return UInt32.MinValue.ToString();
				case CodePage.UInt64LE: return UInt64.MinValue.ToString();
				case CodePage.UInt64BE: return UInt64.MinValue.ToString();
				case CodePage.Single: return Single.MinValue.ToString();
				case CodePage.Double: return Double.MinValue.ToString();
				default: throw new Exception("Invalid type");
			}
		}

		public static string MaxValue(this CodePage codePage)
		{
			switch (codePage)
			{
				case CodePage.Int8: return sbyte.MaxValue.ToString();
				case CodePage.Int16LE: return Int16.MaxValue.ToString();
				case CodePage.Int16BE: return Int16.MaxValue.ToString();
				case CodePage.Int32LE: return Int32.MaxValue.ToString();
				case CodePage.Int32BE: return Int32.MaxValue.ToString();
				case CodePage.Int64LE: return Int64.MaxValue.ToString();
				case CodePage.Int64BE: return Int64.MaxValue.ToString();
				case CodePage.UInt8: return byte.MaxValue.ToString();
				case CodePage.UInt16LE: return UInt16.MaxValue.ToString();
				case CodePage.UInt16BE: return UInt16.MaxValue.ToString();
				case CodePage.UInt32LE: return UInt32.MaxValue.ToString();
				case CodePage.UInt32BE: return UInt32.MaxValue.ToString();
				case CodePage.UInt64LE: return UInt64.MaxValue.ToString();
				case CodePage.UInt64BE: return UInt64.MaxValue.ToString();
				case CodePage.Single: return Single.MaxValue.ToString();
				case CodePage.Double: return Double.MaxValue.ToString();
				default: throw new Exception("Invalid type");
			}
		}

		class NEEncoding
		{
			public CodePage codePage { get; }
			public string shortDescription { get; }
			public string description { get; }
			public Encoding encoding { get; }
			public byte[] preamble { get; }

			internal NEEncoding(EncodingInfo encoding) : this((CodePage)encoding.CodePage, encoding.DisplayName) { }

			internal NEEncoding(CodePage _codePage, string _description)
			{
				codePage = _codePage;
				shortDescription = description = _description;

				if (codePage >= 0)
				{
					if (codePage > 0)
						description += $" - Codepage {(int)codePage}";
					encoding = Encoding.GetEncoding((int)codePage);
					preamble = encoding.GetPreamble();
					if ((preamble != null) && (preamble.Length == 0))
						preamble = null;
				}
			}

			public override string ToString() => description;
		}

		static readonly List<NEEncoding> NEEncodings;
		static readonly Dictionary<CodePage, NEEncoding> NEEncodingDictionary;

		static Coder()
		{
			NEEncodings = new List<NEEncoding>
			{
				new NEEncoding(CodePage.Default, "Default"),
				new NEEncoding(CodePage.ASCII, "ASCII"),
				new NEEncoding(CodePage.UTF8, "UTF8"),
				new NEEncoding(CodePage.UTF16LE, "UTF16 (Little endian)"),
				new NEEncoding(CodePage.UTF16BE, "UTF16 (Big endian)"),
				new NEEncoding(CodePage.UTF32LE, "UTF32 (Little endian)"),
				new NEEncoding(CodePage.UTF32BE, "UTF32 (Big endian)"),
				new NEEncoding(CodePage.AutoUnicode, "Auto Unicode"),
				new NEEncoding(CodePage.AutoByBOM, "Auto By BOM"),
				new NEEncoding(CodePage.Hex, "Hex"),
				new NEEncoding(CodePage.HexRev, "Hex Reversed"),
				new NEEncoding(CodePage.Binary, "Binary"),
				new NEEncoding(CodePage.Base64, "Base64"),
				new NEEncoding(CodePage.Int8, "Int8"),
				new NEEncoding(CodePage.Int16LE, "Int16 (Little endian)"),
				new NEEncoding(CodePage.Int16BE, "Int16 (Big endian)"),
				new NEEncoding(CodePage.Int32LE, "Int32 (Little endian)"),
				new NEEncoding(CodePage.Int32BE, "Int32 (Big endian)"),
				new NEEncoding(CodePage.Int64LE, "Int64 (Little endian)"),
				new NEEncoding(CodePage.Int64BE, "Int64 (Big endian)"),
				new NEEncoding(CodePage.UInt8, "UInt8"),
				new NEEncoding(CodePage.UInt16LE, "UInt16 (Little endian)"),
				new NEEncoding(CodePage.UInt16BE, "UInt16 (Big endian)"),
				new NEEncoding(CodePage.UInt32LE, "UInt32 (Little endian)"),
				new NEEncoding(CodePage.UInt32BE, "UInt32 (Big endian)"),
				new NEEncoding(CodePage.UInt64LE, "UInt64 (Little endian)"),
				new NEEncoding(CodePage.UInt64BE, "UInt64 (Big endian)"),
				new NEEncoding(CodePage.Single, "Single"),
				new NEEncoding(CodePage.Double, "Double"),
			};

			Encoding.GetEncodings().Where(encoding => !NEEncodings.Any(a => a.codePage == (CodePage)encoding.CodePage)).OrderBy(encoding => encoding.DisplayName).ToList().ForEach(encoding => NEEncodings.Add(new NEEncoding(encoding)));

			NEEncodingDictionary = NEEncodings.ToDictionary(encoding => encoding.codePage, encoding => encoding);
		}

		public static int PreviewSize(this CodePage codePage)
		{
			switch (codePage)
			{
				case CodePage.UTF16LE: return 200;
				case CodePage.UTF16BE: return 200;
				case CodePage.UTF32LE: return 400;
				case CodePage.UTF32BE: return 400;
				case CodePage.Binary: return 20;
				default: return 100;
			}
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
			input = input.StripWhitespace();
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
						return null;

					c = *inputPtr++;
					if ((c >= '0') && (c <= '9'))
						*bytesPtr += (byte)(c - '0');
					else if ((c >= 'a') && (c <= 'f'))
						*bytesPtr += (byte)(c - 'a' + 10);
					else if ((c >= 'A') && (c <= 'F'))
						*bytesPtr += (byte)(c - 'A' + 10);
					else
						return null;
				}
			}
			return bytes;
		}

		static unsafe string ToHexRevString(byte[] bytes)
		{
			var hexValue = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };
			var output = new char[bytes.Length * 2];
			fixed (byte* bytesFixed = bytes)
			fixed (char* outputFixed = output)
			{
				char* outputPtr = outputFixed;
				for (var bytesPtr = bytesFixed + bytes.Length - 1; bytesPtr >= bytesFixed; --bytesPtr)
				{
					*outputPtr++ = hexValue[*bytesPtr >> 4];
					*outputPtr++ = hexValue[*bytesPtr & 15];
				}
			}
			return new String(output);
		}

		static unsafe byte[] FromHexRevString(string input)
		{
			input = input.StripWhitespace();
			if ((input.Length % 2) != 0)
				input = '0' + input;
			var bytes = new byte[input.Length >> 1];
			fixed (char* inputFixed = input)
			fixed (byte* bytesFixed = bytes)
			{
				char* inputPtr = inputFixed;
				for (var bytesPtr = bytesFixed + bytes.Length - 1; bytesPtr >= bytesFixed; --bytesPtr)
				{
					var c = *inputPtr++;
					if ((c >= '0') && (c <= '9'))
						*bytesPtr = (byte)((c - '0') << 4);
					else if ((c >= 'a') && (c <= 'f'))
						*bytesPtr = (byte)((c - 'a' + 10) << 4);
					else if ((c >= 'A') && (c <= 'F'))
						*bytesPtr = (byte)((c - 'A' + 10) << 4);
					else
						return null;

					c = *inputPtr++;
					if ((c >= '0') && (c <= '9'))
						*bytesPtr += (byte)(c - '0');
					else if ((c >= 'a') && (c <= 'f'))
						*bytesPtr += (byte)(c - 'a' + 10);
					else if ((c >= 'A') && (c <= 'F'))
						*bytesPtr += (byte)(c - 'A' + 10);
					else
						return null;
				}
			}
			return bytes;
		}

		static unsafe string ToBinaryString(byte[] bytes)
		{
			var output = new char[bytes.Length * 8];
			fixed (byte* bytesFixed = bytes)
			fixed (char* outputFixed = output)
			{
				var len = bytesFixed + bytes.Length;
				char* outputPtr = outputFixed;
				for (var bytesPtr = bytesFixed; bytesPtr < len; ++bytesPtr)
					for (byte mult = 0x80; mult != 0; mult >>= 1)
						*outputPtr++ = (*bytesPtr & mult) == 0 ? '0' : '1';
			}
			return new String(output);
		}

		static unsafe byte[] FromBinaryString(string input)
		{
			input = input.StripWhitespace();
			input = new string('0', 7 - (input.Length + 7) % 8) + input;
			var bytes = new byte[input.Length >> 3];
			fixed (char* inputFixed = input)
			fixed (byte* bytesFixed = bytes)
			{
				char* inputPtr = inputFixed;
				var len = bytesFixed + bytes.Length;
				for (var bytesPtr = bytesFixed; bytesPtr < len; ++bytesPtr)
					for (byte mult = 0x80; mult != 0; mult >>= 1)
						switch (*inputPtr++)
						{
							case '0': break;
							case '1': *bytesPtr |= mult; break;
							default: return null;
						}
			}
			return bytes;
		}

		static string BytesToNum<T>(byte[] data, int size, Func<byte[], T> converter, bool reverse) where T : struct
		{
			var current = new byte[size];
			var result = new List<T>();
			for (var index = 0; index < data.Length; index += size)
			{
				Array.Copy(data, index, current, 0, size);
				if (reverse)
					Array.Reverse(current);
				result.Add(converter(current));
			}
			return String.Join(" ", result);
		}

		public static string TryBytesToString(byte[] data, CodePage codePage, bool stripBOM = false)
		{
			try
			{
				if (codePage == CodePage.AutoUnicode)
					codePage = GuessUnicodeEncoding(data);
				else if (codePage == CodePage.AutoByBOM)
					codePage = CodePageFromBOM(data);

				switch (codePage)
				{
					case CodePage.Int8: return BytesToNum(data, sizeof(sbyte), bytes => (sbyte)bytes[0], false);
					case CodePage.Int16LE: return BytesToNum(data, sizeof(Int16), bytes => BitConverter.ToInt16(bytes, 0), false);
					case CodePage.Int16BE: return BytesToNum(data, sizeof(Int16), bytes => BitConverter.ToInt16(bytes, 0), true);
					case CodePage.Int32LE: return BytesToNum(data, sizeof(Int32), bytes => BitConverter.ToInt32(bytes, 0), false);
					case CodePage.Int32BE: return BytesToNum(data, sizeof(Int32), bytes => BitConverter.ToInt32(bytes, 0), true);
					case CodePage.Int64LE: return BytesToNum(data, sizeof(Int64), bytes => BitConverter.ToInt64(bytes, 0), false);
					case CodePage.Int64BE: return BytesToNum(data, sizeof(Int64), bytes => BitConverter.ToInt64(bytes, 0), true);
					case CodePage.UInt8: return BytesToNum(data, sizeof(byte), bytes => bytes[0], false);
					case CodePage.UInt16LE: return BytesToNum(data, sizeof(UInt16), bytes => BitConverter.ToUInt16(bytes, 0), false);
					case CodePage.UInt16BE: return BytesToNum(data, sizeof(UInt16), bytes => BitConverter.ToUInt16(bytes, 0), true);
					case CodePage.UInt32LE: return BytesToNum(data, sizeof(UInt32), bytes => BitConverter.ToUInt32(bytes, 0), false);
					case CodePage.UInt32BE: return BytesToNum(data, sizeof(UInt32), bytes => BitConverter.ToUInt32(bytes, 0), true);
					case CodePage.UInt64LE: return BytesToNum(data, sizeof(UInt64), bytes => BitConverter.ToUInt64(bytes, 0), false);
					case CodePage.UInt64BE: return BytesToNum(data, sizeof(UInt64), bytes => BitConverter.ToUInt64(bytes, 0), true);
					case CodePage.Single: return BytesToNum(data, sizeof(Single), bytes => BitConverter.ToSingle(bytes, 0), false);
					case CodePage.Double: return BytesToNum(data, sizeof(Double), bytes => BitConverter.ToDouble(bytes, 0), false);
					case CodePage.Base64: return Convert.ToBase64String(data);
					case CodePage.Hex: return ToHexString(data);
					case CodePage.HexRev: return ToHexRevString(data);
					case CodePage.Binary: return ToBinaryString(data);
					default:
						{
							var encoding = NEEncodingDictionary[codePage];
							var start = 0;
							if ((stripBOM) && (encoding.preamble != null) && (data.Length >= encoding.preamble.Length) && (data.Equal(encoding.preamble, encoding.preamble.Length)))
								start = encoding.preamble.Length;
							return encoding.encoding.GetString(data, start, data.Length - start);
						}
				}
			}
			catch { return null; }
			throw new Exception("Invalid conversion");
		}

		public static string BytesToString(byte[] data, CodePage codePage, bool stripBOM = false)
		{
			var result = TryBytesToString(data, codePage, stripBOM);
			if (result == null)
				throw new Exception("Invalid conversion");
			return result;
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

		delegate bool TryParseHandler<T>(string value, out T result);
		static byte[] NumToBytes<T>(string input, TryParseHandler<T> tryParse, Func<T, byte[]> converter, bool reverse)
		{
			var strs = input.Split(',', ' ', '\t', '\r', '\n').Where(str => !String.IsNullOrWhiteSpace(str)).ToList();
			byte[] result = null;
			for (var ctr = 0; ctr < strs.Count; ++ctr)
			{
				T value;
				if (!tryParse(strs[ctr], out value))
					return null;
				var bytes = converter(value);
				if (reverse)
					Array.Reverse(bytes);

				if (result == null)
					result = new byte[strs.Count * bytes.Length];
				Array.Copy(bytes, 0, result, ctr * bytes.Length, bytes.Length);
			}
			return result ?? new byte[0];
		}

		public static byte[] TryStringToBytes(string value, CodePage codePage, bool addBOM = false)
		{
			if (value == null)
				value = "";

			try
			{
				switch (codePage)
				{
					case CodePage.Int8: return NumToBytes<sbyte>(value, SByte.TryParse, v => new byte[] { (byte)v }, false);
					case CodePage.Int16LE: return NumToBytes<Int16>(value, Int16.TryParse, v => BitConverter.GetBytes(v), false);
					case CodePage.Int16BE: return NumToBytes<Int16>(value, Int16.TryParse, v => BitConverter.GetBytes(v), true);
					case CodePage.Int32LE: return NumToBytes<Int32>(value, Int32.TryParse, v => BitConverter.GetBytes(v), false);
					case CodePage.Int32BE: return NumToBytes<Int32>(value, Int32.TryParse, v => BitConverter.GetBytes(v), true);
					case CodePage.Int64LE: return NumToBytes<Int64>(value, Int64.TryParse, v => BitConverter.GetBytes(v), false);
					case CodePage.Int64BE: return NumToBytes<Int64>(value, Int64.TryParse, v => BitConverter.GetBytes(v), true);
					case CodePage.UInt8: return NumToBytes<byte>(value, Byte.TryParse, v => new byte[] { v }, false);
					case CodePage.UInt16LE: return NumToBytes<UInt16>(value, UInt16.TryParse, v => BitConverter.GetBytes(v), false);
					case CodePage.UInt16BE: return NumToBytes<UInt16>(value, UInt16.TryParse, v => BitConverter.GetBytes(v), true);
					case CodePage.UInt32LE: return NumToBytes<UInt32>(value, UInt32.TryParse, v => BitConverter.GetBytes(v), false);
					case CodePage.UInt32BE: return NumToBytes<UInt32>(value, UInt32.TryParse, v => BitConverter.GetBytes(v), true);
					case CodePage.UInt64LE: return NumToBytes<UInt64>(value, UInt64.TryParse, v => BitConverter.GetBytes(v), false);
					case CodePage.UInt64BE: return NumToBytes<UInt64>(value, UInt64.TryParse, v => BitConverter.GetBytes(v), true);
					case CodePage.Single: return NumToBytes<Single>(value, Single.TryParse, v => BitConverter.GetBytes(v), false);
					case CodePage.Double: return NumToBytes<Double>(value, Double.TryParse, v => BitConverter.GetBytes(v), false);
					case CodePage.Base64: return GetBase64Bytes(value);
					case CodePage.Hex: return FromHexString(value);
					case CodePage.HexRev: return FromHexRevString(value);
					case CodePage.Binary: return FromBinaryString(value);
					default: return NEEncodingDictionary[codePage].encoding.GetBytes(((addBOM) && (NEEncodingDictionary[codePage].preamble != null) ? "\ufeff" : "") + value);
				}
			}
			catch { return null; }
			throw new Exception("Invalid conversion");
		}

		public static byte[] StringToBytes(string value, CodePage codePage, bool addBOM = false)
		{
			var result = TryStringToBytes(value, codePage, addBOM);
			if (result == null)
				throw new Exception("Invalid conversion");
			return result;
		}

		public static bool CanFullyEncode(string str1, CodePage codePage)
		{
			// These formats will allow whitespace, although you can't save it
			if ((str1 != null) && ((codePage == CodePage.Hex) || (codePage == CodePage.HexRev) || (codePage == CodePage.Binary) || (codePage == CodePage.Base64)))
				str1 = str1.StripWhitespace();

			var bytes = TryStringToBytes(str1, codePage);
			if (bytes == null)
				return false;
			var str2 = TryBytesToString(bytes, codePage);
			if (str2 == null)
				return false;
			return str1 == str2;
		}

		public static bool CanFullyEncode(byte[] bytes1, CodePage codePage)
		{
			var str = Coder.TryBytesToString(bytes1, codePage);
			if (str == null)
				return false;
			var bytes2 = Coder.TryStringToBytes(str, codePage);
			if (bytes2 == null)
				return false;
			if (bytes1.Length != bytes2.Length)
				return false;
			return bytes1.Equal(bytes2);
		}

		public static CodePage CodePageFromBOM(string fileName)
		{
			using (var file = File.OpenRead(fileName))
			{
				var header = new byte[Math.Min(4, file.Length)];
				file.Read(header, 0, header.Length);
				return Coder.CodePageFromBOM(header);
			}
		}

		public static CodePage CodePageFromBOM(byte[] data)
		{
			var encodingWithPreambles = NEEncodings.Where(encoding => encoding.preamble != null).OrderByDescending(encoding => encoding.preamble.Length).ToList();

			foreach (var encoding in encodingWithPreambles)
			{
				var match = true;
				if (data.Length < encoding.preamble.Length)
					match = false;
				if (match)
					for (var ctr = 0; ctr < encoding.preamble.Length; ctr++)
						if (data[ctr] != encoding.preamble[ctr])
							match = false;
				if (match)
					return encoding.codePage;
			}

			return CodePage.Default;
		}

		public static CodePage GuessUnicodeEncoding(byte[] data)
		{
			var encoding = CodePageFromBOM(data);
			if (encoding != CodePage.Default)
				return encoding;

			if (data.Length >= 4)
			{
				if (BitConverter.ToUInt32(data, 0) <= 0xffff)
					return CodePage.UTF32LE;
				if (BitConverter.ToUInt32(data.Take(4).Reverse().ToArray(), 0) <= 0xffff)
					return CodePage.UTF32BE;
			}

			var zeroIndex = Array.IndexOf(data, (byte)0);
			if (zeroIndex == -1)
				return CodePage.UTF8;
			else if ((zeroIndex & 1) == 1)
				return CodePage.UTF16LE;
			else
				return CodePage.UTF16BE;
		}

		public static List<CodePage> GetAllCodePages() => NEEncodings.Select(encoding => encoding.codePage).ToList();
		public static List<CodePage> GetStringCodePages() => GetAllCodePages().Where(codePage => IsStr(codePage)).ToList();
		public static List<CodePage> GetNumericCodePages() => GetAllCodePages().Where(codePage => !IsStr(codePage)).ToList();

		public static string GetDescription(CodePage codePage, bool shortDescription = false)
		{
			if (shortDescription)
				return NEEncodingDictionary[codePage].shortDescription;
			return NEEncodingDictionary[codePage].description;
		}

		public static bool AlwaysCaseSensitive(CodePage codePage) => (codePage == CodePage.Hex) || (codePage == CodePage.HexRev) || (codePage == CodePage.Base64);

		public static Encoding GetEncoding(CodePage codePage) => NEEncodingDictionary[codePage].encoding;

		public static bool IsStr(CodePage codePage) => codePage >= CodePage.StartString;

		public static int PreambleSize(CodePage codePage)
		{
			switch (codePage)
			{
				case CodePage.UTF8: return 3;
				case CodePage.UTF16LE: return 2;
				case CodePage.UTF16BE: return 2;
				case CodePage.UTF32LE: return 4;
				case CodePage.UTF32BE: return 4;
				default: return 0;
			}
		}

		public static int CharSize(CodePage codePage)
		{
			switch (codePage)
			{
				case CodePage.UTF16LE: return 2;
				case CodePage.UTF16BE: return 2;
				case CodePage.UTF32LE: return 4;
				case CodePage.UTF32BE: return 4;
				default: return 1;
			}
		}
	}
}
