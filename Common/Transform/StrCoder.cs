using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeoEdit.Common.Transform
{
	public static class StrCoder
	{
		public enum CodePage
		{
			None = -1000000,
			AutoByBOM = -3,
			AutoUnicode = -4,
			UTF8 = 65001,
			UTF16LE = 1200,
			UTF16BE = 1201,
			UTF32LE = 12000,
			UTF32BE = 12001,
			Default = 0,
			Hex = -1,
			Base64 = -2,
		}

		class NEEncoding
		{
			public CodePage codePage { get; private set; }
			public string description { get; private set; }
			public Encoding encoding { get; private set; }
			public byte[] preamble { get; private set; }

			internal NEEncoding(EncodingInfo encoding) : this((CodePage)encoding.CodePage, encoding.DisplayName) { }

			internal NEEncoding(CodePage codePage, string description)
			{
				this.codePage = codePage;
				this.description = description;

				if (codePage >= 0)
				{
					if (codePage > 0)
						description += " - Codepage " + codePage;
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

		static StrCoder()
		{
			NEEncodings = new List<NEEncoding>
			{
				new NEEncoding(CodePage.UTF8, "UTF8"),
				new NEEncoding(CodePage.UTF16LE, "UTF16 (Little endian)"),
				new NEEncoding(CodePage.UTF16BE, "UTF16 (Big endian)"),
				new NEEncoding(CodePage.UTF32LE, "UTF32 (Little endian)"),
				new NEEncoding(CodePage.UTF32BE, "UTF32 (Big endian)"),
				new NEEncoding(CodePage.Default, "Default"),
				new NEEncoding(CodePage.Hex, "Hex"),
				new NEEncoding(CodePage.Base64, "Base64"),
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

		public static string BytesToString(byte[] data, CodePage codePage, bool stripBOM = false)
		{
			if (codePage == CodePage.AutoUnicode)
				codePage = GuessUnicodeEncoding(data);
			else if (codePage == CodePage.AutoByBOM)
				codePage = CodePageFromBOM(data);

			switch (codePage)
			{
				case CodePage.Base64: return Convert.ToBase64String(data).TrimEnd('=');
				case CodePage.Hex: return ToHexString(data);
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

		public static byte[] TryStringToBytes(string value, CodePage codePage, bool addBOM = false)
		{
			if (value == null)
				value = "";

			try
			{
				switch (codePage)
				{
					case CodePage.Base64: return GetBase64Bytes(value);
					case CodePage.Hex: return FromHexString(value);
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

		public static List<Tuple<CodePage, string>> GetEncodingTypes()
		{
			return NEEncodings.Select(encoding => new Tuple<CodePage, string>(encoding.codePage, encoding.description)).ToList();
		}
	}
}
