using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.GUI
{
	static class Helpers
	{
		public static bool IsIntegerType(this Type type)
		{
			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
					return true;
				default:
					return false;
			}
		}

		public static IEnumerable<T> GetValues<T>()
		{
			return Enum.GetValues(typeof(T)).Cast<T>();
		}

		public static T ParseEnum<T>(string str)
		{
			return (T)Enum.Parse(typeof(T), str);
		}

		public static unsafe long ForwardArraySearch(byte[] data, long index, byte[] find, bool ignoreCase)
		{
			fixed (byte* dataFixed = data)
			fixed (byte* findStart = find)
			{
				var dataStart = dataFixed + index;
				var dataEnd = dataFixed + data.LongLength - find.LongLength + 1;
				var findEnd = findStart + find.LongLength;

				for (byte* innerDataPtr = dataStart; innerDataPtr < dataEnd; ++innerDataPtr)
				{
					var findPtr = findStart;
					for (byte* dataPtr = innerDataPtr; findPtr < findEnd; ++findPtr, ++dataPtr)
					{
						if (*findPtr == *dataPtr)
							continue;
						if (!ignoreCase)
							break;
						if ((*dataPtr >= 'a') && (*dataPtr <= 'z') && (*findPtr >= 'A') && (*findPtr <= 'Z') && (*dataPtr - 'a' + 'A' == *findPtr))
							continue;
						if ((*dataPtr >= 'A') && (*dataPtr <= 'Z') && (*findPtr >= 'a') && (*findPtr <= 'z') && (*dataPtr - 'A' + 'a' == *findPtr))
							continue;
						break;
					}

					if (findPtr == findEnd)
						return innerDataPtr - dataFixed;
				}

				return -1;
			}
		}

		public static unsafe long BackwardArraySearch(byte[] data, long index, byte[] find, bool ignoreCase)
		{
			fixed (byte* dataFixed = data)
			fixed (byte* findStart = find)
			{
				var dataStart = dataFixed + Math.Min(index, data.LongLength - find.LongLength);
				var findEnd = findStart + find.LongLength;

				for (byte* innerDataPtr = dataStart; innerDataPtr >= dataFixed; --innerDataPtr)
				{
					var findPtr = findStart;
					for (byte* dataPtr = innerDataPtr; findPtr < findEnd; ++findPtr, ++dataPtr)
					{
						if (*findPtr == *dataPtr)
							continue;
						if (!ignoreCase)
							break;
						if ((*dataPtr >= 'a') && (*dataPtr <= 'z') && (*findPtr >= 'A') && (*findPtr <= 'Z') && (*dataPtr - 'a' + 'A' == *findPtr))
							continue;
						if ((*dataPtr >= 'A') && (*dataPtr <= 'Z') && (*findPtr >= 'a') && (*findPtr <= 'z') && (*dataPtr - 'A' + 'a' == *findPtr))
							continue;
						break;
					}

					if (findPtr == findEnd)
						return innerDataPtr - dataFixed;
				}

				return -1;
			}
		}
	}
}
