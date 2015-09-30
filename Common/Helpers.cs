using System;
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

		public static string ToSpacedHex(this long input, int len = 16, int spacing = 4)
		{
			var format = String.Format(String.Format("{{0:X{0}}}", len), input);
			for (var space = len - spacing; space > 0; space -= spacing)
				format = format.Substring(0, space) + " " + format.Substring(space);
			return format;
		}

		public static bool Equal(this byte[] b1, byte[] b2)
		{
			if (b1.Length != b2.Length)
				return false;
			return Equal(b1, b2, 0, 0, b1.Length);
		}

		public static unsafe bool Equal(this byte[] b1, byte[] b2, int count)
		{
			return Equal(b1, b2, 0, 0, count);
		}

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

		public static IEnumerable<TResult> GetNth<TResult>(this IEnumerable<TResult> source, int n)
		{
			var ctr = 0;
			foreach (var item in source)
			{
				if (ctr == 0)
				{
					yield return item;
					ctr = n;
				}
				--ctr;
			}
		}

		public static IEnumerable<TSource> Recurse<TSource>(this TSource source, Func<TSource, TSource> recurse)
		{
			var items = new Queue<TSource>();
			if (source != null)
				items.Enqueue(source);
			while (items.Any())
			{
				var item = items.Dequeue();
				yield return item;
				var child = recurse(item);
				if (child != null)
					items.Enqueue(child);
			}
		}

		public static IEnumerable<TSource> Resize<TSource>(this IEnumerable<TSource> source, int count, TSource expandWith)
		{
			return source.Take(count).Expand(count, expandWith);
		}

		public static IEnumerable<TSource> Expand<TSource>(this IEnumerable<TSource> source, int count, TSource expandWith)
		{
			foreach (var item in source)
			{
				yield return item;
				--count;
			}
			for (; count > 0; --count)
				yield return expandWith;
		}

		public static IEnumerable<TSource> Distinct<TSource, TData>(this IEnumerable<TSource> source, Func<TSource, TData> selector)
		{
			var seen = new HashSet<TData>();
			foreach (var item in source)
			{
				var data = selector(item);
				if (seen.Contains(data))
					continue;
				seen.Add(data);
				yield return item;
			}
		}

		public static bool InOrder<TSource>(this IEnumerable<TSource> source, bool ascending = true, bool equal = false) where TSource : IComparable
		{
			var prev = default(TSource);
			bool hasPrev = false;
			foreach (var current in source)
			{
				if (hasPrev)
				{
					var compare = current.CompareTo(prev);
					if ((compare < 0) && (ascending))
						return false;
					if ((compare == 0) && (!equal))
						return false;
					if ((compare > 0) && (!ascending))
						return false;
				}

				prev = current;
				hasPrev = true;
			}
			return true;
		}

		[DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern unsafe int memcmp(byte* b1, byte* b2, long count);

		public static string NeoEditAppData { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NeoEdit"); } }

		static Helpers() { Directory.CreateDirectory(NeoEditAppData); }
	}
}
