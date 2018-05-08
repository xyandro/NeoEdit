using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
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

			StartImage = -200,
			Bitmap = -201,
			GIF = -202,
			JPEG = -203,
			PNG = -204,
			TIFF = -205,
			EndImage = -206,

			StartString = -50,

			AutoUnicode = -40,
			AutoByBOM = -41,

			StartNonAutoString = -30,

			Hex = -3,
			HexRev = -4,
			Binary = -5,
			Base64 = -6,

			ASCII = 437,
			CodePage1252 = 1252,
			UTF8 = 65001,
			UTF16LE = 1200,
			UTF16BE = 1201,
			UTF32LE = 12000,
			UTF32BE = 12001,
		}

		public static CodePage DefaultCodePage { get; } = (CodePage)Encoding.Default.CodePage;

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
				case CodePage.Int16LE: return short.MinValue.ToString();
				case CodePage.Int16BE: return short.MinValue.ToString();
				case CodePage.Int32LE: return int.MinValue.ToString();
				case CodePage.Int32BE: return int.MinValue.ToString();
				case CodePage.Int64LE: return long.MinValue.ToString();
				case CodePage.Int64BE: return long.MinValue.ToString();
				case CodePage.UInt8: return byte.MinValue.ToString();
				case CodePage.UInt16LE: return ushort.MinValue.ToString();
				case CodePage.UInt16BE: return ushort.MinValue.ToString();
				case CodePage.UInt32LE: return uint.MinValue.ToString();
				case CodePage.UInt32BE: return uint.MinValue.ToString();
				case CodePage.UInt64LE: return ulong.MinValue.ToString();
				case CodePage.UInt64BE: return ulong.MinValue.ToString();
				case CodePage.Single: return float.MinValue.ToString();
				case CodePage.Double: return double.MinValue.ToString();
				default: throw new Exception("Invalid type");
			}
		}

		public static string MaxValue(this CodePage codePage)
		{
			switch (codePage)
			{
				case CodePage.Int8: return sbyte.MaxValue.ToString();
				case CodePage.Int16LE: return short.MaxValue.ToString();
				case CodePage.Int16BE: return short.MaxValue.ToString();
				case CodePage.Int32LE: return int.MaxValue.ToString();
				case CodePage.Int32BE: return int.MaxValue.ToString();
				case CodePage.Int64LE: return long.MaxValue.ToString();
				case CodePage.Int64BE: return long.MaxValue.ToString();
				case CodePage.UInt8: return byte.MaxValue.ToString();
				case CodePage.UInt16LE: return ushort.MaxValue.ToString();
				case CodePage.UInt16BE: return ushort.MaxValue.ToString();
				case CodePage.UInt32LE: return uint.MaxValue.ToString();
				case CodePage.UInt32BE: return uint.MaxValue.ToString();
				case CodePage.UInt64LE: return ulong.MaxValue.ToString();
				case CodePage.UInt64BE: return ulong.MaxValue.ToString();
				case CodePage.Single: return float.MaxValue.ToString();
				case CodePage.Double: return double.MaxValue.ToString();
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

			internal NEEncoding(CodePage _codePage, string _description, string _preamble = null)
			{
				codePage = _codePage;
				shortDescription = description = _description;

				if (!string.IsNullOrWhiteSpace(_preamble))
					preamble = Coder.StringToBytes(_preamble, CodePage.Hex);

				if (codePage >= 0)
				{
					if (codePage > 0)
						description += $" - Codepage {(int)codePage}";
					if (codePage == DefaultCodePage)
						description = $"Default - {description}";
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
			var encodings = Encoding.GetEncodings().Select(encoding => new NEEncoding(encoding)).OrderBy(encoding => encoding.description).ToList();
			var defaultEncoding = encodings.Single(encoding => encoding.codePage == DefaultCodePage);

			NEEncodings = new List<NEEncoding>
			{
				defaultEncoding,
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
				new NEEncoding(CodePage.Bitmap, "Bitmap", "424D"),
				new NEEncoding(CodePage.GIF, "GIF", "474946"),
				new NEEncoding(CodePage.JPEG, "JPEG", "FFD8"),
				new NEEncoding(CodePage.PNG, "PNG", "89504E47"),
				new NEEncoding(CodePage.TIFF, "TIFF"),
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

			NEEncodings.AddRange(encodings.Where(encoding => !NEEncodings.Any(a => a.codePage == encoding.codePage)));

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
			return new string(output);
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
			return new string(output);
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
			return new string(output);
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

		static unsafe string ToImageString(byte[] data)
		{
			if (data.Length == 0)
				return "";

			Bitmap bitmap;
			using (var ms = new MemoryStream(data))
				bitmap = new Bitmap(ms);

			var lockBits = bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
			var hexNumbers = Enumerable.Range(0, 16).Select(num => $"{num:x}").ToArray();
			try
			{
				var padding = lockBits.Stride - lockBits.Width * 4;
				var bytes = (byte*)lockBits.Scan0.ToPointer();

				var ending = "\r\n";
				var sb = new StringBuilder(lockBits.Height * (lockBits.Width * 9 - 1 + ending.Length));

				for (var y = 0; y < lockBits.Height; ++y)
				{
					for (var x = 0; x < lockBits.Width; ++x)
					{
						if (x != 0)
							sb.Append(" ");
						var value = *(uint*)bytes;
						for (var bit = 28; bit >= 0; bit -= 4)
							sb.Append(hexNumbers[value >> bit & 15]);
						bytes += 4;
					}
					sb.Append(ending);
					bytes += padding;
				}

				return sb.ToString();
			}
			finally
			{
				bitmap.UnlockBits(lockBits);
			}
		}

		static unsafe byte[] FromImageString(string data, ImageFormat imageFormat)
		{
			data = ReformatImage(data);
			if (data.Length == 0)
				return new byte[0];

			var height = 0;
			var width = 1;
			for (var ctr = 0; ctr < data.Length; ++ctr)
			{
				if ((height == 0) && (data[ctr] == ' '))
					++width;
				if (data[ctr] == '\r')
					++height;
			}

			var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
			var lockBits = bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), ImageLockMode.WriteOnly, bitmap.PixelFormat);
			try
			{
				var padding = lockBits.Stride - lockBits.Width * 4;
				var bytes = (byte*)lockBits.Scan0.ToPointer();
				var dataIndex = 0;

				for (var y = 0; y < height; ++y)
				{
					for (var x = 0; x < width; ++x)
					{
						uint value = 0;
						for (var ctr = 0; ctr < 8; ++ctr)
						{
							value *= 16;
							var c = data[dataIndex];
							if (c >= '0' && c <= '9')
								value += (uint)(c - '0');
							else
								value += (uint)(c - 'a' + 10);
							++dataIndex;
						}
						*(uint*)bytes = value;
						++dataIndex;
						bytes += 4;
					}
					bytes += padding;
					++dataIndex;
				}
			}
			finally
			{
				bitmap.UnlockBits(lockBits);
			}
			using (var ms = new MemoryStream())
			{
				bitmap.Save(ms, imageFormat);
				return ms.ToArray();
			}
		}

		static string ReformatImage(string str)
		{
			str = str.ToLowerInvariant();
			var width = default(int?);
			var lineWidth = 0;
			var sb = new StringBuilder();
			for (var ctr = 0; ctr <= str.Length;)
			{
				var endCtr = ctr;
				while ((endCtr < str.Length) && (((str[endCtr] >= '0') && (str[endCtr] <= '9')) || ((str[endCtr] >= 'a') && (str[endCtr] <= 'f'))))
					++endCtr;

				if (endCtr != ctr)
				{
					if ((sb.Length != 0) && (sb[sb.Length - 1] != '\n'))
						sb.Append(" ");

					switch (endCtr - ctr)
					{
						case 1: sb.Append("ff").Append(str[ctr], 6); break;
						case 2: sb.Append("ff").Append(str, ctr, 2).Append(str, ctr, 2).Append(str, ctr, 2); break;
						case 3: sb.Append("ff").Append(str[ctr], 2).Append(str[ctr + 1], 2).Append(str[ctr + 2], 2); break;
						case 4: sb.Append(str[ctr], 2).Append(str[ctr + 1], 2).Append(str[ctr + 2], 2).Append(str[ctr + 3], 2); break;
						case 6: sb.Append("ff").Append(str, ctr, 6); break;
						case 8: sb.Append(str, ctr, 8); break;
						default: throw new Exception("Invalid color");
					}
					ctr = endCtr;
					++lineWidth;
					continue;
				}

				if ((ctr == str.Length) || (str[ctr] == '\r') || (str[ctr] == '\n'))
				{
					if ((sb.Length != 0) && (sb[sb.Length - 1] != '\n'))
					{
						width = width ?? lineWidth;
						if (lineWidth != width)
							throw new Exception("All lines must have the same number of pixels");
						lineWidth = 0;
						sb.Append("\r\n");
					}
					++ctr;
					continue;
				}

				if (char.IsWhiteSpace(str[ctr]))
				{
					++ctr;
					continue;
				}

				throw new Exception("Invalid image string");
			}
			return sb.ToString();
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
			return string.Join(" ", result);
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
					case CodePage.Int16LE: return BytesToNum(data, sizeof(short), bytes => BitConverter.ToInt16(bytes, 0), false);
					case CodePage.Int16BE: return BytesToNum(data, sizeof(short), bytes => BitConverter.ToInt16(bytes, 0), true);
					case CodePage.Int32LE: return BytesToNum(data, sizeof(int), bytes => BitConverter.ToInt32(bytes, 0), false);
					case CodePage.Int32BE: return BytesToNum(data, sizeof(int), bytes => BitConverter.ToInt32(bytes, 0), true);
					case CodePage.Int64LE: return BytesToNum(data, sizeof(long), bytes => BitConverter.ToInt64(bytes, 0), false);
					case CodePage.Int64BE: return BytesToNum(data, sizeof(long), bytes => BitConverter.ToInt64(bytes, 0), true);
					case CodePage.UInt8: return BytesToNum(data, sizeof(byte), bytes => bytes[0], false);
					case CodePage.UInt16LE: return BytesToNum(data, sizeof(ushort), bytes => BitConverter.ToUInt16(bytes, 0), false);
					case CodePage.UInt16BE: return BytesToNum(data, sizeof(ushort), bytes => BitConverter.ToUInt16(bytes, 0), true);
					case CodePage.UInt32LE: return BytesToNum(data, sizeof(uint), bytes => BitConverter.ToUInt32(bytes, 0), false);
					case CodePage.UInt32BE: return BytesToNum(data, sizeof(uint), bytes => BitConverter.ToUInt32(bytes, 0), true);
					case CodePage.UInt64LE: return BytesToNum(data, sizeof(ulong), bytes => BitConverter.ToUInt64(bytes, 0), false);
					case CodePage.UInt64BE: return BytesToNum(data, sizeof(ulong), bytes => BitConverter.ToUInt64(bytes, 0), true);
					case CodePage.Single: return BytesToNum(data, sizeof(float), bytes => BitConverter.ToSingle(bytes, 0), false);
					case CodePage.Double: return BytesToNum(data, sizeof(double), bytes => BitConverter.ToDouble(bytes, 0), false);
					case CodePage.Base64: return Convert.ToBase64String(data);
					case CodePage.Hex: return ToHexString(data);
					case CodePage.HexRev: return ToHexRevString(data);
					case CodePage.Binary: return ToBinaryString(data);
					case CodePage.Bitmap: return ToImageString(data);
					case CodePage.GIF: return ToImageString(data);
					case CodePage.JPEG: return ToImageString(data);
					case CodePage.PNG: return ToImageString(data);
					case CodePage.TIFF: return ToImageString(data);
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
				else if (char.IsWhiteSpace(value[ctr]))
					continue; // Skip whitespace
				else if (value[ctr] == '=')
					padding = true;
				else if (padding)
					return null; // No more chars allowed after padding starts
				else if ((char.IsLetterOrDigit(value[ctr])) || (value[ctr] == '+') || (value[ctr] == '/'))
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
			var strs = input.Split(',', ' ', '\t', '\r', '\n').Where(str => !string.IsNullOrWhiteSpace(str)).ToList();
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
					case CodePage.Int8: return NumToBytes<sbyte>(value, sbyte.TryParse, v => new byte[] { (byte)v }, false);
					case CodePage.Int16LE: return NumToBytes<short>(value, short.TryParse, v => BitConverter.GetBytes(v), false);
					case CodePage.Int16BE: return NumToBytes<short>(value, short.TryParse, v => BitConverter.GetBytes(v), true);
					case CodePage.Int32LE: return NumToBytes<int>(value, int.TryParse, v => BitConverter.GetBytes(v), false);
					case CodePage.Int32BE: return NumToBytes<int>(value, int.TryParse, v => BitConverter.GetBytes(v), true);
					case CodePage.Int64LE: return NumToBytes<long>(value, long.TryParse, v => BitConverter.GetBytes(v), false);
					case CodePage.Int64BE: return NumToBytes<long>(value, long.TryParse, v => BitConverter.GetBytes(v), true);
					case CodePage.UInt8: return NumToBytes<byte>(value, byte.TryParse, v => new byte[] { v }, false);
					case CodePage.UInt16LE: return NumToBytes<ushort>(value, ushort.TryParse, v => BitConverter.GetBytes(v), false);
					case CodePage.UInt16BE: return NumToBytes<ushort>(value, ushort.TryParse, v => BitConverter.GetBytes(v), true);
					case CodePage.UInt32LE: return NumToBytes<uint>(value, uint.TryParse, v => BitConverter.GetBytes(v), false);
					case CodePage.UInt32BE: return NumToBytes<uint>(value, uint.TryParse, v => BitConverter.GetBytes(v), true);
					case CodePage.UInt64LE: return NumToBytes<ulong>(value, ulong.TryParse, v => BitConverter.GetBytes(v), false);
					case CodePage.UInt64BE: return NumToBytes<ulong>(value, ulong.TryParse, v => BitConverter.GetBytes(v), true);
					case CodePage.Single: return NumToBytes<float>(value, float.TryParse, v => BitConverter.GetBytes(v), false);
					case CodePage.Double: return NumToBytes<double>(value, double.TryParse, v => BitConverter.GetBytes(v), false);
					case CodePage.Base64: return GetBase64Bytes(value);
					case CodePage.Hex: return FromHexString(value);
					case CodePage.HexRev: return FromHexRevString(value);
					case CodePage.Binary: return FromBinaryString(value);
					case CodePage.Bitmap: return FromImageString(value, ImageFormat.Bmp);
					case CodePage.GIF: return FromImageString(value, ImageFormat.Gif);
					case CodePage.JPEG: return FromImageString(value, ImageFormat.Jpeg);
					case CodePage.PNG: return FromImageString(value, ImageFormat.Png);
					case CodePage.TIFF: return FromImageString(value, ImageFormat.Tiff);
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

		public static string ConvertString(string value, CodePage inCodePage, CodePage outCodePage) => BytesToString(StringToBytes(value, inCodePage), outCodePage);

		public static bool CanFullyEncode(string str1, CodePage codePage)
		{
			// Handle formatting (whitespace/case/etc)
			if ((str1 != null) && ((codePage == CodePage.Hex) || (codePage == CodePage.HexRev) || (codePage == CodePage.Binary) || (codePage == CodePage.Base64)))
				str1 = str1.StripWhitespace();
			if ((str1 != null) && ((codePage == CodePage.Hex) || (codePage == CodePage.HexRev)))
				str1 = str1.ToLowerInvariant();
			if ((str1 != null) && (codePage == CodePage.Base64))
				str1 = str1.TrimEnd('=');
			if ((str1 != null) && (IsImage(codePage)))
				str1 = ReformatImage(str1);
			if ((str1 != null) && (IsNumeric(codePage)))
				str1 = str1.ConvertWhitespaceToSpaces().Trim();

			var bytes = TryStringToBytes(str1, codePage);
			if (bytes == null)
				return false;
			var str2 = TryBytesToString(bytes, codePage);
			if (str2 == null)
				return false;

			if (codePage == CodePage.Base64)
				str2 = str2.TrimEnd('=');

			return str1 == str2;
		}

		public static bool CanFullyEncode(byte[] bytes1, CodePage codePage)
		{
			var str = TryBytesToString(bytes1, codePage);
			if (str == null)
				return false;
			var bytes2 = TryStringToBytes(str, codePage);
			if (bytes2 == null)
				return false;
			if (bytes1.Length != bytes2.Length)
				return false;
			return bytes1.Equal(bytes2);
		}

		public static CodePage CodePageFromBOM(string fileName)
		{
			var maxPreambleLength = NEEncodings.Select(encoding => encoding.preamble).NonNull().Select(preamble => preamble.Length).Max();
			using (var file = File.OpenRead(fileName))
			{
				var header = new byte[Math.Min(maxPreambleLength, file.Length)];
				file.Read(header, 0, header.Length);
				return CodePageFromBOM(header);
			}
		}

		public static CodePage CodePageFromBOM(byte[] data) => NEEncodings.NonNull(encoding => encoding.preamble).OrderByDescending(encoding => encoding.preamble.Length).Where(encoding => (data.Length >= encoding.preamble.Length) && (data.Equal(encoding.preamble, encoding.preamble.Length))).Select(encoding => encoding.codePage).DefaultIfEmpty(DefaultCodePage).First();

		public static CodePage GuessUnicodeEncoding(byte[] data)
		{
			var encoding = CodePageFromBOM(data);
			if (encoding != DefaultCodePage)
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

		public static bool IsImage(CodePage codePage) => (codePage <= CodePage.StartImage) && (codePage >= CodePage.EndImage);

		public static bool IsNumeric(CodePage codePage) => (codePage <= CodePage.StartNum) && (codePage >= CodePage.EndNum);

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
