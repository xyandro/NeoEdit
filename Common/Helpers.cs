﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NeoEdit.Common
{
	public static class Helpers
	{
		public static bool IsDebugBuild
		{
			get
			{
#if DEBUG
				return true;
#else
				return false;
#endif
			}
		}

		public static bool IsIntegerType(this Type type)
		{
			if ((type.IsGenericType) && (type.GetGenericTypeDefinition() == typeof(Nullable<>)))
				type = Nullable.GetUnderlyingType(type);

			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.UInt16:
				case TypeCode.Int16:
				case TypeCode.UInt32:
				case TypeCode.Int32:
				case TypeCode.UInt64:
				case TypeCode.Int64:
					return true;
				default:
					return false;
			}
		}

		public static bool IsFloatType(this Type type)
		{
			if ((type.IsGenericType) && (type.GetGenericTypeDefinition() == typeof(Nullable<>)))
				type = Nullable.GetUnderlyingType(type);

			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.Decimal:
					return true;
				default:
					return false;
			}
		}

		public static bool IsNumericType(this Type type) => (IsIntegerType(type)) || (IsFloatType(type));

		public static bool IsDateType(this Type type)
		{
			if ((type.IsGenericType) && (type.GetGenericTypeDefinition() == typeof(Nullable<>)))
				type = Nullable.GetUnderlyingType(type);

			return Type.GetTypeCode(type) == TypeCode.DateTime;
		}

		public static IEnumerable<T> GetValues<T>() => Enum.GetValues(typeof(T)).Cast<T>();

		public static T ParseEnum<T>(string str) => (T)Enum.Parse(typeof(T), str);

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

		public static string CoalesceNullOrEmpty(this string input, params string[] inputs) => new[] { input }.Concat(inputs).FirstOrDefault(str => !string.IsNullOrEmpty(str));
		public static string CoalesceNullOrEmpty(params string[] inputs) => inputs.FirstOrDefault(str => !string.IsNullOrEmpty(str));
		public static string CoalesceNullOrWhiteSpace(this string input, params string[] inputs) => string.IsNullOrWhiteSpace(input) ? CoalesceNullOrWhiteSpace(inputs) : input;
		public static string CoalesceNullOrWhiteSpace(params string[] inputs) => inputs.FirstOrDefault(str => !string.IsNullOrWhiteSpace(str));

		public static string ToSpacedHex(this long input, int len = 16, int spacing = 4)
		{
			var format = String.Format(String.Format("{{0:X{0}}}", len), input);
			for (var space = len - spacing; space > 0; space -= spacing)
				format = $"{format.Substring(0, space)} {format.Substring(space)}";
			return format;
		}

		public static bool Equal(this byte[] b1, byte[] b2)
		{
			if (b1.Length != b2.Length)
				return false;
			return Equal(b1, b2, 0, 0, b1.Length);
		}

		public static unsafe bool Equal(this byte[] b1, byte[] b2, int count) => Equal(b1, b2, 0, 0, count);

		public static unsafe bool Equal(this byte[] b1, byte[] b2, int offset1, int offset2, int count)
		{
			if (count == 0)
				return true;
			if ((offset1 + count > b1.Length) || (offset2 + count > b2.Length))
				throw new ArgumentOutOfRangeException();
			fixed (byte* p1 = &b1[0])
			fixed (byte* p2 = &b2[0])
				return memcmp(p1 + offset1, p2 + offset2, count) == 0;
		}

		public static void PartitionedParallelForEach(int count, int partitionSize, Action<int, int> action)
		{
			var chunks = new List<Tuple<int, int>>();
			for (var start = 0; start < count;)
			{
				var end = Math.Min(count, start + partitionSize);
				chunks.Add(Tuple.Create(start, end));
				start = end;
			}
			Parallel.ForEach(chunks, chunk => action(chunk.Item1, chunk.Item2));
		}

		public static List<TResult> PartitionedParallelForEach<TResult>(int count, int partitionSize, Action<int, int, List<TResult>> action)
		{
			var chunks = new List<Tuple<int, int>>();
			for (var start = 0; start < count;)
			{
				var end = Math.Min(count, start + partitionSize);
				chunks.Add(Tuple.Create(start, end));
				start = end;
			}
			var lists = chunks.Select(chunk => new List<TResult>()).ToList();
			Parallel.ForEach(chunks, (chunk, state, index) => action(chunk.Item1, chunk.Item2, lists[(int)index]));
			var result = new List<TResult>();
			lists.ForEach(list => result.AddRange(list));
			return result;
		}

		static public IEnumerable<string> SplitByLine(this string item)
		{
			var lineBreakChars = new char[] { '\r', '\n' };
			var pos = 0;
			while (pos < item.Length)
			{
				var index = item.IndexOfAny(lineBreakChars, pos);
				if (index == -1)
					index = item.Length;
				yield return item.Substring(pos, index - pos);
				if ((index + 1 < item.Length) && (item[index] == '\r') && (item[index + 1] == '\n'))
					++index;
				pos = index + 1;
			}
		}

		static List<string> SplitTCSV(string source, char splitChar, ref int pos)
		{
			var findChars = new char[] { splitChar, '\r', '\n' };
			var result = new List<string>();
			while (true)
			{
				var item = "";
				if ((pos < source.Length) && (source[pos] == '"'))
				{
					var quoteIndex = pos + 1;
					while (true)
					{
						quoteIndex = source.IndexOf('"', quoteIndex);
						if (quoteIndex == -1)
						{
							item += source.Substring(pos + 1).Replace(@"""""", @"""");
							pos = source.Length;
							break;
						}

						if ((quoteIndex + 1 < source.Length) && (source[quoteIndex + 1] == '"'))
						{
							quoteIndex += 2;
							continue;
						}

						item += source.Substring(pos + 1, quoteIndex - pos - 1).Replace(@"""""", @"""");
						pos = quoteIndex + 1;
						break;
					}
				}

				var splitIndex = source.IndexOfAny(findChars, pos);
				var end = (splitIndex == -1) || (source[splitIndex] != splitChar);
				if (splitIndex == -1)
					splitIndex = source.Length;
				item += source.Substring(pos, splitIndex - pos);
				result.Add(item);

				pos = splitIndex;

				if (end)
				{
					if ((pos + 1 < source.Length) && (source[pos] == '\r') && (source[pos + 1] == '\n'))
						pos += 2;
					else if (pos < source.Length)
						++pos;
					break;
				}
				++pos;
			}
			return result;
		}

		static public IEnumerable<List<string>> SplitTCSV(this string source, char splitChar)
		{
			var pos = 0;
			while (pos < source.Length)
				yield return SplitTCSV(source, splitChar, ref pos);
			yield break;
		}

		[DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern unsafe int memcmp(byte* b1, byte* b2, long count);

		public static string NeoEditAppData => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NeoEdit");

		static Helpers() { Directory.CreateDirectory(NeoEditAppData); }
	}
}
