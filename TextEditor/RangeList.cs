using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NeoEdit.TextEditor
{
	static class RangeListExtensions
	{
		public static ObservableCollection<Range> ToList(this IEnumerable<Range> source)
		{
			return new ObservableCollection<Range>(source);
		}

		public static void AddRange(this ObservableCollection<Range> ranges, IEnumerable<Range> items)
		{
			foreach (var item in items)
				ranges.Add(item);
		}

		public static void RemoveRange(this ObservableCollection<Range> ranges, int index, int count)
		{
			for (var ctr = 0; ctr < count; ++ctr)
				ranges.RemoveAt(index);
		}

		public static void Replace(this ObservableCollection<Range> ranges, IEnumerable<Range> items)
		{
			ranges.Clear();
			ranges.AddRange(items);
		}

		public static void Replace(this ObservableCollection<Range> ranges, Range item)
		{
			ranges.Clear();
			ranges.Add(item);
		}

		public static void ForEach(this ObservableCollection<Range> ranges, Action<Range> action)
		{
			foreach (var item in ranges)
				action(item);
		}

		public static int FindIndex(this ObservableCollection<Range> ranges, Predicate<Range> match)
		{
			var ctr = 0;
			foreach (var range in ranges)
			{
				if (match(range))
					return ctr;
				++ctr;
			}
			return -1;
		}

		public static int BinaryFindFirst(this ObservableCollection<Range> ranges, Predicate<Range> predicate)
		{
			int min = 0, max = ranges.Count;
			while (min < max)
			{
				int mid = (min + max) / 2;
				if (predicate(ranges[mid]))
					max = mid;
				else
					min = mid + 1;
			}
			return min == ranges.Count ? -1 : min;
		}

		public static int BinaryFindLast(this ObservableCollection<Range> ranges, Predicate<Range> predicate)
		{
			int min = -1, max = ranges.Count - 1;
			while (min < max)
			{
				int mid = (min + max + 1) / 2;
				if (predicate(ranges[mid]))
					min = mid;
				else
					max = mid - 1;
			}
			return min;
		}
	}
}
