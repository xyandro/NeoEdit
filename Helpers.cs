using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit
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
	}
}
