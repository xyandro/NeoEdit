using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit
{
	static class Helpers
	{
		public static bool ArrayEqual<T>(this T[] array1, T[] array2, int num = -1, int offset1 = 0, int offset2 = 0)
		{
			if (num == -1)
			{
				var size1 = array1.Length - offset1;
				var size2 = array2.Length - offset2;
				if (size1 != size2)
					return false;
				num = size1;
			}
			if (array1.Length < num)
				return false;
			if (array2.Length < num)
				return false;
			for (var ctr = 0; ctr < num; ctr++)
				if (!array1[offset1 + ctr].Equals(array2[offset2 + ctr]))
					return false;
			return true;
		}

		public static int IndexOf<T>(this IEnumerable<T> source, T find)
		{
			var index = 0;
			foreach (var item in source)
			{
				if (item.Equals(find))
					return index;
				++index;
			}
			return -1;
		}

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
	}
}
