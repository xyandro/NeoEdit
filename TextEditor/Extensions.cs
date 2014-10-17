using System;
using System.Collections.Generic;

namespace NeoEdit.TextEditor
{
	static class Extensions
	{
		public static void AddRange<T>(this IList<T> source, IEnumerable<T> items)
		{
			foreach (var item in items)
				source.Add(item);
		}

		public static void RemoveRange<T>(this IList<T> source, int index, int count)
		{
			for (var ctr = 0; ctr < count; ++ctr)
				source.RemoveAt(index);
		}

		public static void Replace<T>(this IList<T> source, IEnumerable<T> items)
		{
			source.Clear();
			source.AddRange(items);
		}

		public static void Replace<T>(this IList<T> source, T item)
		{
			source.Clear();
			source.Add(item);
		}

		public static int FindIndex<T>(this IList<T> source, Predicate<T> match)
		{
			var ctr = 0;
			foreach (var range in source)
			{
				if (match(range))
					return ctr;
				++ctr;
			}
			return -1;
		}

		public static int BinaryFindFirst<T>(this IList<T> source, Predicate<T> predicate)
		{
			int min = 0, max = source.Count;
			while (min < max)
			{
				int mid = (min + max) / 2;
				if (predicate(source[mid]))
					max = mid;
				else
					min = mid + 1;
			}
			return min == source.Count ? -1 : min;
		}

		public static int BinaryFindLast<T>(this IList<T> source, Predicate<T> predicate)
		{
			int min = -1, max = source.Count - 1;
			while (min < max)
			{
				int mid = (min + max + 1) / 2;
				if (predicate(source[mid]))
					min = mid;
				else
					max = mid - 1;
			}
			return min;
		}
	}
}
