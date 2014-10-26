using System;

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
		};

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
	}
}
