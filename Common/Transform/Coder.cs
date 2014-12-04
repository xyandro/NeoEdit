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
			Binary = -4,
			Base64 = -5,

			Default = 0,
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

		static byte[] Resize(byte[] data, long count, bool littleEndian)
		{
			var ret = new byte[count];
			count = Math.Min(count, data.Length);
			Array.Copy(data, 0, ret, 0, count);
			if (!littleEndian)
				Array.Reverse(ret, 0, (int)count);
			return ret;
		}

		class NEEncoding
		{
			public CodePage codePage { get; private set; }
			public string shortDescription { get; private set; }
			public string description { get; private set; }
			public Encoding encoding { get; private set; }
			public byte[] preamble { get; private set; }

			internal NEEncoding(EncodingInfo encoding) : this((CodePage)encoding.CodePage, encoding.DisplayName) { }

			internal NEEncoding(CodePage _codePage, string _description)
			{
				codePage = _codePage;
				shortDescription = description = _description;

				if (codePage >= 0)
				{
					if (codePage > 0)
						description += " - Codepage " + (int)codePage;
					encoding = Encoding.GetEncoding((int)codePage);
					preamble = encoding.GetPreamble();
					if ((preamble != null) && (preamble.Length == 0))
						preamble = null;
				}
			}

			public override string ToString() { return description; }
		}

		static readonly List<NEEncoding> NEEncodings;
		static readonly Dictionary<CodePage, NEEncoding> NEEncodingDictionary;

		static Coder()
		{
			NEEncodings = new List<NEEncoding>
			{
				new NEEncoding(CodePage.Default, "Default"),
				new NEEncoding(CodePage.UTF8, "UTF8"),
				new NEEncoding(CodePage.UTF16LE, "UTF16 (Little endian)"),
				new NEEncoding(CodePage.UTF16BE, "UTF16 (Big endian)"),
				new NEEncoding(CodePage.UTF32LE, "UTF32 (Little endian)"),
				new NEEncoding(CodePage.UTF32BE, "UTF32 (Big endian)"),
				new NEEncoding(CodePage.AutoUnicode, "Auto Unicode"),
				new NEEncoding(CodePage.AutoByBOM, "Auto By BOM"),
				new NEEncoding(CodePage.Hex, "Hex"),
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
					case CodePage.Int8: return ((sbyte)Resize(data, 1, true)[0]).ToString();
					case CodePage.Int16LE: return BitConverter.ToInt16(Resize(data, 2, true), 0).ToString();
					case CodePage.Int16BE: return BitConverter.ToInt16(Resize(data, 2, false), 0).ToString();
					case CodePage.Int32LE: return BitConverter.ToInt32(Resize(data, 4, true), 0).ToString();
					case CodePage.Int32BE: return BitConverter.ToInt32(Resize(data, 4, false), 0).ToString();
					case CodePage.Int64LE: return BitConverter.ToInt64(Resize(data, 8, true), 0).ToString();
					case CodePage.Int64BE: return BitConverter.ToInt64(Resize(data, 8, false), 0).ToString();
					case CodePage.UInt8: return Resize(data, 1, true)[0].ToString();
					case CodePage.UInt16LE: return BitConverter.ToUInt16(Resize(data, 2, true), 0).ToString();
					case CodePage.UInt16BE: return BitConverter.ToUInt16(Resize(data, 2, false), 0).ToString();
					case CodePage.UInt32LE: return BitConverter.ToUInt32(Resize(data, 4, true), 0).ToString();
					case CodePage.UInt32BE: return BitConverter.ToUInt32(Resize(data, 4, false), 0).ToString();
					case CodePage.UInt64LE: return BitConverter.ToUInt64(Resize(data, 8, true), 0).ToString();
					case CodePage.UInt64BE: return BitConverter.ToUInt64(Resize(data, 8, false), 0).ToString();
					case CodePage.Single: return BitConverter.ToSingle(Resize(data, 4, true), 0).ToString();
					case CodePage.Double: return BitConverter.ToDouble(Resize(data, 8, true), 0).ToString();
					case CodePage.Base64: return Convert.ToBase64String(data);
					case CodePage.Hex: return ToHexString(data);
					case CodePage.Binary: return ToBinaryString(data);
					default:
						{
							var encoding = NEEncodingDictionary[codePage];
							var result = encoding.encoding.GetString(data);
							if ((stripBOM) && (encoding.preamble != null) && (result.StartsWith("\ufeff")))
								result = result.Substring(1);
							return result;
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
			if ((str1 != null) && ((codePage == CodePage.Hex) || (codePage == CodePage.Binary) || (codePage == CodePage.Base64)))
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
			for (var ctr = 0; ctr < bytes1.Length; ++ctr)
				if (bytes1[ctr] != bytes2[ctr])
					return false;
			return true;
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

		public static List<CodePage> GetCodePages(bool stringsOnly = true)
		{
			var result = NEEncodings.Select(encoding => encoding.codePage).ToList();
			if (stringsOnly)
				result = result.Where(codePage => codePage >= CodePage.StartNonAutoString).ToList();
			return result;
		}

		public static string GetDescription(CodePage codePage, bool shortDescription = false)
		{
			if (shortDescription)
				return NEEncodingDictionary[codePage].shortDescription;
			return NEEncodingDictionary[codePage].description;
		}

		public static bool AlwaysCaseSensitive(CodePage codePage)
		{
			return (codePage == CodePage.Hex) || (codePage == CodePage.Base64);
		}

		public static Encoding GetEncoding(CodePage codePage)
		{
			return NEEncodingDictionary[codePage].encoding;
		}

		public static bool IsStr(CodePage codePage)
		{
			return codePage >= CodePage.StartString;
		}

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
