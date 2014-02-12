using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace NeoEdit.Common
{
	public class BinaryData : DependencyObject
	{
		[DepProp]
		public long ChangeCount { get { return uiHelper.GetPropValue<long>(); } set { uiHelper.SetPropValue(value); } }

		public enum EncodingName
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

		public static bool IsStr(EncodingName converter)
		{
			switch (converter)
			{
				case EncodingName.UTF7:
				case EncodingName.UTF8:
				case EncodingName.UTF16LE:
				case EncodingName.UTF16BE:
				case EncodingName.UTF32LE:
				case EncodingName.UTF32BE:
					return true;
				default:
					return false;
			}
		}

		public static int PreviewSize(EncodingName converter)
		{
			switch (converter)
			{
				case EncodingName.UTF16LE: return 200;
				case EncodingName.UTF16BE: return 200;
				case EncodingName.UTF32LE: return 400;
				case EncodingName.UTF32BE: return 400;
				default: return 100;
			}
		}

		static BinaryData() { UIHelper<BinaryData>.Register(); }

		readonly UIHelper<BinaryData> uiHelper;
		byte[] data;
		public BinaryData(byte[] _data)
		{
			uiHelper = new UIHelper<BinaryData>(this);
			data = _data;
		}

		public byte this[long index]
		{
			get { return data[index]; }
		}

		public long Length
		{
			get { return data.Length; }
		}

		public static implicit operator BinaryData(byte[] data)
		{
			return new BinaryData(data);
		}

		public override string ToString()
		{
			return BitConverter.ToString(data);
		}

		public string MD5()
		{
			using (var md5 = System.Security.Cryptography.MD5.Create())
				return BitConverter.ToString(md5.ComputeHash(data)).Replace("-", "").ToLower();
		}

		BinaryData GetBytes(long index, long numBytes, long count, bool littleEndian)
		{
			var ret = new byte[count];
			count = Math.Min(count, Length - index);
			if (numBytes != 0)
				count = Math.Min(count, numBytes);
			Array.Copy(data, index, ret, 0, count);
			if (!littleEndian)
				Array.Reverse(ret, 0, (int)count);
			return new BinaryData(ret);
		}

		string GetString(Encoding encoding, long index, long numBytes)
		{
			try
			{
				if (numBytes == 0)
					numBytes = Length - index;
				numBytes = Math.Min(numBytes, Length - index);
				var str = encoding.GetString(data, (int)index, (int)numBytes);
				return str;
			}
			catch { return "Failed"; }
		}

		static char GetHexChar(int val)
		{
			if ((val >= 0) && (val < 10))
				return (char)('0' + val);
			return (char)('A' + val - 10);
		}

		public string ToHexString()
		{
			return ToHexString(0, 0, false);
		}

		public string ToHexString(long index, long numBytes, bool reverse)
		{
			if (numBytes == 0)
				numBytes = Length - index;
			numBytes = Math.Min(Length - index, numBytes);
			var sb = new StringBuilder((int)(numBytes * 2));
			for (var ctr = 0; ctr < numBytes; ctr++)
			{
				var ch = reverse ? data[index + numBytes - ctr - 1] : data[index + ctr];
				sb.Append(GetHexChar(ch >> 4));
				sb.Append(GetHexChar(ch & 15));
			}
			return sb.ToString();
		}

		public string ToString(EncodingName type)
		{
			return ToString(type, 0, Length);
		}

		public static Encoding GetEncoding(EncodingName type)
		{
			switch (type)
			{
				case EncodingName.UTF7: return Encoding.UTF7;
				case EncodingName.UTF8: return Encoding.UTF8;
				case EncodingName.UTF16LE: return Encoding.Unicode;
				case EncodingName.UTF16BE: return Encoding.BigEndianUnicode;
				case EncodingName.UTF32LE: return Encoding.UTF32;
				case EncodingName.UTF32BE: return new UTF32Encoding(true, true);
				default: return null;
			}
		}

		public string ToString(EncodingName type, long index, long numBytes)
		{
			switch (type)
			{
				case EncodingName.UInt8LE: return GetBytes(index, numBytes, 1, true)[0].ToString();
				case EncodingName.UInt16LE: return BitConverter.ToUInt16(GetBytes(index, numBytes, 2, true).data, 0).ToString();
				case EncodingName.UInt32LE: return BitConverter.ToUInt32(GetBytes(index, numBytes, 4, true).data, 0).ToString();
				case EncodingName.UInt64LE: return BitConverter.ToUInt64(GetBytes(index, numBytes, 8, true).data, 0).ToString();
				case EncodingName.Int8LE: return ((sbyte)GetBytes(index, numBytes, 1, true)[0]).ToString();
				case EncodingName.Int16LE: return BitConverter.ToInt16(GetBytes(index, numBytes, 2, true).data, 0).ToString();
				case EncodingName.Int32LE: return BitConverter.ToInt32(GetBytes(index, numBytes, 4, true).data, 0).ToString();
				case EncodingName.Int64LE: return BitConverter.ToInt64(GetBytes(index, numBytes, 8, true).data, 0).ToString();
				case EncodingName.UInt8BE: return GetBytes(index, numBytes, 1, false)[0].ToString();
				case EncodingName.UInt16BE: return BitConverter.ToUInt16(GetBytes(index, numBytes, 2, false).data, 0).ToString();
				case EncodingName.UInt32BE: return BitConverter.ToUInt32(GetBytes(index, numBytes, 4, false).data, 0).ToString();
				case EncodingName.UInt64BE: return BitConverter.ToUInt64(GetBytes(index, numBytes, 8, false).data, 0).ToString();
				case EncodingName.Int8BE: return ((sbyte)GetBytes(index, numBytes, 1, false)[0]).ToString();
				case EncodingName.Int16BE: return BitConverter.ToInt16(GetBytes(index, numBytes, 2, false).data, 0).ToString();
				case EncodingName.Int32BE: return BitConverter.ToInt32(GetBytes(index, numBytes, 4, false).data, 0).ToString();
				case EncodingName.Int64BE: return BitConverter.ToInt64(GetBytes(index, numBytes, 8, false).data, 0).ToString();
				case EncodingName.Single: return BitConverter.ToSingle(GetBytes(index, numBytes, 4, true).data, 0).ToString();
				case EncodingName.Double: return BitConverter.ToDouble(GetBytes(index, numBytes, 8, true).data, 0).ToString();
				case EncodingName.UTF7:
				case EncodingName.UTF8:
				case EncodingName.UTF16LE:
				case EncodingName.UTF16BE:
				case EncodingName.UTF32LE:
				case EncodingName.UTF32BE:
					return GetString(GetEncoding(type), index, numBytes);
				case EncodingName.Base64: return Convert.ToBase64String(data);
				case EncodingName.Hex: return ToHexString(index, numBytes, false);
				case EncodingName.HexRev: return ToHexString(index, numBytes, true);
			}
			throw new Exception("Invalid conversion");
		}

		public delegate bool TryParseHandler<T>(string value, out T result);
		static BinaryData NumToBytes<T>(string str, TryParseHandler<T> tryParse, Func<T, byte[]> converter, bool reverse)
		{
			T value;
			if (!tryParse(str, out value))
				return null;
			var data = converter(value);
			if (reverse)
				Array.Reverse(data);
			return new BinaryData(data);
		}

		static int GetHexValue(char c)
		{
			if ((c >= '0') && (c <= '9'))
				return c - '0';
			return c - 'A' + 10;
		}

		static BinaryData StringToHex(string str, bool reverse)
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
			return new BinaryData(ret);
		}

		public static BinaryData FromString(EncodingName type, string value)
		{
			if (value == null)
				value = "";

			switch (type)
			{
				case EncodingName.UInt8LE: return NumToBytes<byte>(value, Byte.TryParse, v => new byte[] { v }, false);
				case EncodingName.UInt16LE: return NumToBytes<UInt16>(value, UInt16.TryParse, v => BitConverter.GetBytes(v), false);
				case EncodingName.UInt32LE: return NumToBytes<UInt32>(value, UInt32.TryParse, v => BitConverter.GetBytes(v), false);
				case EncodingName.UInt64LE: return NumToBytes<UInt64>(value, UInt64.TryParse, v => BitConverter.GetBytes(v), false);
				case EncodingName.Int8LE: return NumToBytes<sbyte>(value, SByte.TryParse, v => new byte[] { (byte)v }, false);
				case EncodingName.Int16LE: return NumToBytes<Int16>(value, Int16.TryParse, v => BitConverter.GetBytes(v), false);
				case EncodingName.Int32LE: return NumToBytes<Int32>(value, Int32.TryParse, v => BitConverter.GetBytes(v), false);
				case EncodingName.Int64LE: return NumToBytes<Int64>(value, Int64.TryParse, v => BitConverter.GetBytes(v), false);
				case EncodingName.UInt8BE: return NumToBytes<byte>(value, Byte.TryParse, v => new byte[] { v }, true);
				case EncodingName.UInt16BE: return NumToBytes<UInt16>(value, UInt16.TryParse, v => BitConverter.GetBytes(v), true);
				case EncodingName.UInt32BE: return NumToBytes<UInt32>(value, UInt32.TryParse, v => BitConverter.GetBytes(v), true);
				case EncodingName.UInt64BE: return NumToBytes<UInt64>(value, UInt64.TryParse, v => BitConverter.GetBytes(v), true);
				case EncodingName.Int8BE: return NumToBytes<sbyte>(value, SByte.TryParse, v => new byte[] { (byte)v }, true);
				case EncodingName.Int16BE: return NumToBytes<Int16>(value, Int16.TryParse, v => BitConverter.GetBytes(v), true);
				case EncodingName.Int32BE: return NumToBytes<Int32>(value, Int32.TryParse, v => BitConverter.GetBytes(v), true);
				case EncodingName.Int64BE: return NumToBytes<Int64>(value, Int64.TryParse, v => BitConverter.GetBytes(v), true);
				case EncodingName.Single: return NumToBytes<Single>(value, Single.TryParse, v => BitConverter.GetBytes(v), false);
				case EncodingName.Double: return NumToBytes<Double>(value, Double.TryParse, v => BitConverter.GetBytes(v), false);
				case EncodingName.UTF7:
				case EncodingName.UTF8:
				case EncodingName.UTF16LE:
				case EncodingName.UTF16BE:
				case EncodingName.UTF32LE:
				case EncodingName.UTF32BE:
					return new BinaryData(GetEncoding(type).GetBytes(value));
				case EncodingName.Base64: return new BinaryData(Convert.FromBase64String(value.TrimStart('\ufeff')));
				case EncodingName.Hex: return StringToHex(value, false);
				case EncodingName.HexRev: return StringToHex(value, true);
			}
			throw new Exception("Invalid conversion");
		}

		public bool Find(FindData currentFind, long index, out long start, out long end, bool forward = true)
		{
			start = end = -1;
			var offset = forward ? 1 : -1;
			Func<byte, long, long> findFunc;
			if (forward)
			{
				findFunc = (_find, _start) =>
				{
					var _pos = Array.IndexOf(data, _find, (int)_start);
					if (_pos == -1)
						return long.MaxValue;
					return _pos;
				};
			}
			else
			{
				findFunc = (_find, _start) => Array.LastIndexOf(data, _find, (int)_start);
			}
			var selectFunc = forward ? (Func<long, long, long>)Math.Min : Math.Max;
			var invalid = forward ? long.MaxValue : -1;

			var pos = index;
			while (true)
			{
				pos += offset;
				if ((pos < 0) || (pos >= Length))
					return false;

				var usePos = invalid;
				for (var findPos = 0; findPos < currentFind.Data.Count; findPos++)
				{
					var ignoreCase = currentFind.IgnoreCase[findPos];
					var findData = currentFind.Data[findPos];

					usePos = selectFunc(usePos, findFunc(findData[0], pos));
					if (ignoreCase)
					{
						if ((findData[0] >= 'a') && (findData[0] <= 'z'))
							usePos = selectFunc(usePos, findFunc((byte)(findData[0] - 'a' + 'A'), pos));
						else if ((findData[0] >= 'A') && (findData[0] <= 'Z'))
							usePos = selectFunc(usePos, findFunc((byte)(findData[0] - 'A' + 'a'), pos));
					}
				}

				pos = usePos;
				if ((usePos < 0) || (usePos >= Length))
					return false;

				for (var findPos = 0; findPos < currentFind.Data.Count; findPos++)
				{
					var ignoreCase = currentFind.IgnoreCase[findPos];
					var findData = currentFind.Data[findPos];

					int findIdx;
					for (findIdx = 0; findIdx < findData.Length; ++findIdx)
					{
						if (pos + findIdx >= Length)
							break;

						if (data[pos + findIdx] == findData[findIdx])
							continue;

						if (!ignoreCase)
							break;

						if ((data[pos + findIdx] >= 'a') && (data[pos + findIdx] <= 'z') && (findData[findIdx] >= 'A') && (findData[findIdx] <= 'Z'))
							if (data[pos + findIdx] - 'a' + 'A' == findData[findIdx])
								continue;

						if ((data[pos + findIdx] >= 'A') && (data[pos + findIdx] <= 'Z') && (findData[findIdx] >= 'a') && (findData[findIdx] <= 'z'))
							if (data[pos + findIdx] - 'A' + 'a' == findData[findIdx])
								continue;

						break;
					}

					if (findIdx == findData.Length)
					{
						start = pos;
						end = pos + findData.Length - 1;
						return true;
					}
				}
			}
		}

		public void Replace(long index, BinaryData bytes)
		{
			var count = Math.Min(bytes.Length, Length - index);
			for (var ctr = 0; ctr < count; ctr++)
				data[index + ctr] = bytes[ctr];
			++ChangeCount;
		}

		public void Delete(long index, long count)
		{
			if (index < 0)
				return;

			count = Math.Min(count, Length - index);
			var newData = new byte[Length - count];
			Array.Copy(data, 0, newData, 0, index);
			Array.Copy(data, index + count, newData, index, Length - index - count);
			data = newData;
			++ChangeCount;
		}

		public void Insert(long index, BinaryData bytes)
		{
			var newData = new byte[Length + bytes.Length];
			Array.Copy(data, 0, newData, 0, index);
			Array.Copy(bytes.data, 0, newData, index, bytes.Length);
			Array.Copy(data, index, newData, index + bytes.Length, Length - index);
			data = newData;
			++ChangeCount;
		}

		public EncodingName GuessEncoding()
		{
			var preambles = Helpers.GetValues<EncodingName>().Where(a => a.IsStr()).Select(a => new { type = a, preamble = FromString(a, "\ufeff") }).OrderByDescending(a => a.preamble.Length).ToDictionary(a => a.type, a => a.preamble);

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
					return EncodingName.UTF32LE;
				if (BitConverter.ToUInt32(data.Take(4).Reverse().ToArray(), 0) <= 0xffff)
					return EncodingName.UTF32BE;
			}

			var zeroIndex = Array.IndexOf(data, (byte)0);
			if (zeroIndex == -1)
				return EncodingName.UTF8;
			else if ((zeroIndex & 1) == 1)
				return EncodingName.UTF16LE;
			else
				return EncodingName.UTF16BE;
		}

		public BinaryData GetSubset(long index, long count)
		{
			index = Math.Max(0, Math.Min(data.Length - 1, index));
			count = Math.Max(0, Math.Min(data.Length - index, count));
			return data.Skip((int)index).Take((int)count).ToArray();
		}
	}

	public static class BinaryDataExtensions
	{
		public static bool IsStr(this BinaryData.EncodingName type)
		{
			return BinaryData.IsStr(type);
		}
	}
}
