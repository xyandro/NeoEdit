using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeoEdit.Common
{
	public static class Helpers
	{
		public static bool IsIntegerType(this Type type)
		{
			if ((type.IsGenericType) && (type.GetGenericTypeDefinition() == typeof(Nullable<>)))
				type = Nullable.GetUnderlyingType(type);

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

		public static bool IsDateType(this Type type)
		{
			if ((type.IsGenericType) && (type.GetGenericTypeDefinition() == typeof(Nullable<>)))
				type = Nullable.GetUnderlyingType(type);

			return Type.GetTypeCode(type) == TypeCode.DateTime;
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

		public static unsafe string ToProper(this string input)
		{
			var output = new char[input.Length];
			fixed (char* inputFixed = input)
			fixed (char* outputFixed = output)
			{
				var len = inputFixed + input.Length;
				bool doUpper = true;
				char* outputPtr, inputPtr;
				for (inputPtr = inputFixed, outputPtr = outputFixed; inputPtr < len; ++inputPtr, ++outputPtr)
				{
					var c = *inputPtr;
					var nextDoUpper = false;
					if ((c >= 'a') && (c <= 'z'))
					{
						if (doUpper)
							*outputPtr = (char)(c - 'a' + 'A');
						else
							*outputPtr = c;
					}
					else if ((c >= 'A') && (c <= 'Z'))
					{
						if (doUpper)
							*outputPtr = c;
						else
							*outputPtr = (char)(c - 'A' + 'a');
					}
					else
					{
						*outputPtr = c;
						nextDoUpper = true;
					}

					doUpper = nextDoUpper;
				}
			}
			return new String(output);
		}

		public static unsafe string ToToggled(this string input)
		{
			var output = new char[input.Length];
			fixed (char* inputFixed = input)
			fixed (char* outputFixed = output)
			{
				var len = inputFixed + input.Length;
				char* outputPtr, inputPtr;
				for (inputPtr = inputFixed, outputPtr = outputFixed; inputPtr < len; ++inputPtr, ++outputPtr)
				{
					var c = *inputPtr;
					if ((c >= 'a') && (c <= 'z'))
						*outputPtr = (char)(c - 'a' + 'A');
					else if ((c >= 'A') && (c <= 'Z'))
						*outputPtr = (char)(c - 'A' + 'a');
					else
						*outputPtr = c;
				}
			}
			return new String(output);
		}

		public static bool IsNumeric(this string input)
		{
			var inputLen = input.Length;
			if (inputLen == 0)
				return false;
			for (var ctr = 0; ctr < inputLen; ++ctr)
				if (!Char.IsDigit(input[ctr]))
					return false;
			return true;
		}

		public static string StripWhitespace(this string input)
		{
			var sb = new StringBuilder();
			foreach (var c in input)
				if (!Char.IsWhiteSpace(c))
					sb.Append(c);
			return sb.ToString();
		}
	}
}
