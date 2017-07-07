using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Common
{
	public static class MoreLinq
	{
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

		public static IEnumerable<TSource> Resize<TSource>(this IEnumerable<TSource> source, int count, TSource expandWith) => source.Take(count).Expand(count, expandWith);

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

		public static IEnumerable<TSource> Duplicate<TSource, TData>(this IEnumerable<TSource> source, Func<TSource, TData> selector)
		{
			var seen = new HashSet<TData>();
			foreach (var item in source)
			{
				var data = selector(item);
				if (seen.Contains(data))
					yield return item;
				else
					seen.Add(data);
			}
		}

		public static IEnumerable<TSource> Match<TSource, TData>(this IEnumerable<TSource> source, Func<TSource, TData> selector)
		{
			var previous = default(TData);
			foreach (var item in source)
			{
				var data = selector(item);
				if (Equals(previous, data))
					yield return item;
				else
					previous = data;
			}
		}

		public static IEnumerable<TSource> NonMatch<TSource, TData>(this IEnumerable<TSource> source, Func<TSource, TData> selector)
		{
			var previous = default(TData);
			foreach (var item in source)
			{
				var data = selector(item);
				if (Equals(previous, data))
					continue;
				previous = data;
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

		public static IEnumerable<int> Indexes<TSource>(this IEnumerable<TSource> source, Predicate<TSource> predicate)
		{
			var index = 0;
			foreach (var item in source)
			{
				if (predicate(item))
					yield return index;
				++index;
			}
		}

		public static bool Matches<TSource>(this IEnumerable<TSource> source1, IEnumerable<TSource> source2)
		{
			using (var enum1 = source1.GetEnumerator())
			using (var enum2 = source2.GetEnumerator())
			{
				while (true)
				{
					var move1 = enum1.MoveNext();
					var move2 = enum2.MoveNext();
					if (move1 != move2)
						return false;
					if (!move1)
						return true;

					if (!Equals(enum1.Current, enum2.Current))
						return false;
				}
			}
		}

		public static IEnumerable<TSource> Concat<TSource>(this IEnumerable<TSource> source, TSource item)
		{
			foreach (var i in source)
				yield return i;
			yield return item;
		}

		public static IOrderedEnumerable<TSource> OrderBy<TSource>(this IEnumerable<TSource> source) => source.OrderBy(a => a);
		public static IOrderedEnumerable<TSource> OrderBy<TSource>(this IEnumerable<TSource> source, IComparer<TSource> comparer) => source.OrderBy(a => a, comparer);
		public static IOrderedEnumerable<TSource> OrderByDescending<TSource>(this IEnumerable<TSource> source) => source.OrderByDescending(a => a);
		public static IOrderedEnumerable<TSource> OrderByDescending<TSource>(this IEnumerable<TSource> source, IComparer<TSource> comparer) => source.OrderByDescending(a => a, comparer);

		public static bool All<TSource>(this IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
		{
			var index = 0;
			foreach (var item in source)
			{
				if (!predicate(item, index))
					return false;
				++index;
			}
			return true;
		}

		public static bool Any<TSource>(this IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
		{
			var index = 0;
			foreach (var item in source)
			{
				if (predicate(item, index))
					return true;
				++index;
			}
			return false;
		}

		public static IEnumerable<IEnumerable<TSource>> Batch<TSource>(this IEnumerable<TSource> source, int batchSize)
		{
			List<TSource> batch = null;

			foreach (var item in source)
			{
				if (batch == null)
					batch = new List<TSource>();

				batch.Add(item);

				if (batch.Count == batchSize)
				{
					yield return batch;
					batch = null;
				}
			}

			if (batch != null)
				yield return batch;
		}

		public static void ForEach<TSource>(this IEnumerable<TSource> source, Action<TSource> action)
		{
			foreach (var item in source)
				action(item);
		}

		public static void ForEach<TSource>(this IEnumerable<TSource> source, Action<TSource, int> action)
		{
			var ctr = 0;
			foreach (var item in source)
				action(item, ctr++);
		}

		public static List<TResult> ForEach<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> action)
		{
			var result = new List<TResult>();
			foreach (var item in source)
				result.Add(action(item));
			return result;
		}

		public static List<TResult> ForEach<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, TResult> action)
		{
			var ctr = 0;
			var result = new List<TResult>();
			foreach (var item in source)
				result.Add(action(item, ctr++));
			return result;
		}

		public static IEnumerable<TSource> Null<TSource>(this IEnumerable<TSource> source) => source.Where(item => item == null);
		public static IEnumerable<TSource> NonNull<TSource>(this IEnumerable<TSource> source) where TSource : class => source.Where(item => item != null);
		public static IEnumerable<TSource> NonNull<TSource, TElement>(this IEnumerable<TSource> source, Func<TSource, TElement> selector) where TSource : class => source.Where(item => selector(item) != null);
		public static IEnumerable<TSource> NonNull<TSource>(this IEnumerable<TSource?> source) where TSource : struct => source.Where(item => item.HasValue).Select(item => item.Value);

		public static IEnumerable<string> NullOrEmpty(this IEnumerable<string> source) => source.Where(str => string.IsNullOrEmpty(str));
		public static IEnumerable<TSource> NullOrEmpty<TSource>(this IEnumerable<TSource> source, Func<TSource, string> selector) => source.Where(obj => string.IsNullOrEmpty(selector(obj)));
		public static IEnumerable<string> NullOrWhiteSpace(this IEnumerable<string> source) => source.Where(str => string.IsNullOrWhiteSpace(str));
		public static IEnumerable<TSource> NullOrWhiteSpace<TSource>(this IEnumerable<TSource> source, Func<TSource, string> selector) => source.Where(obj => string.IsNullOrWhiteSpace(selector(obj)));
		public static IEnumerable<string> NonNullOrEmpty(this IEnumerable<string> source) => source.Where(str => !string.IsNullOrEmpty(str));
		public static IEnumerable<TSource> NonNullOrEmpty<TSource>(this IEnumerable<TSource> source, Func<TSource, string> selector) => source.Where(obj => !string.IsNullOrEmpty(selector(obj)));
		public static IEnumerable<string> NonNullOrWhiteSpace(this IEnumerable<string> source) => source.Where(str => !string.IsNullOrWhiteSpace(str));
		public static IEnumerable<TSource> NonNullOrWhiteSpace<TSource>(this IEnumerable<TSource> source, Func<TSource, string> selector) => source.Where(obj => !string.IsNullOrWhiteSpace(selector(obj)));

		public static void Execute(this IEnumerable<Action> source) => source.ForEach(action => action());
		public static List<TResult> Execute<TResult>(this IEnumerable<Func<TResult>> source) => source.ForEach(action => action());

		public static IEnumerable<TSource> Coalesce<TSource>(this IEnumerable<TSource> source, TSource defaultValue) where TSource : class => source.Select(val => val ?? defaultValue);
		public static IEnumerable<TSource> Coalesce<TSource>(this IEnumerable<TSource?> source, TSource defaultValue) where TSource : struct => source.Select(val => val ?? defaultValue);

		public static IEnumerable<TSource> EveryNth<TSource>(this IEnumerable<TSource> source, int n, int count = 1)
		{
			if (n < 1)
				throw new Exception("n must be at least 1");
			if (count < 1)
				throw new Exception("count must be at least 1");
			if (count > n)
				throw new Exception("count must be less than n");

			var skip = 0;
			var take = count;
			foreach (var item in source)
			{
				if (skip > 0)
				{
					--skip;
					continue;
				}
				if (take > 0)
				{
					yield return item;
					--take;
					if (take == 0)
					{
						skip = n - count;
						take = count;
					}
				}
			}
		}

		public static string ToJoinedString<TSource>(this IEnumerable<TSource> source, string separator = "") => string.Join(separator, source);

		public static bool All(this IEnumerable<bool> source) => source.All(b => b);
		public static bool Any(this IEnumerable<bool> source) => source.Any(b => b);

		public static Tuple<TSource, TValue> MinByTuple<TSource, TValue>(this IEnumerable<TSource> source, Func<TSource, TValue> selector) where TValue : IComparable
		{
			var minItem = default(TSource);
			var minValue = default(TValue);
			bool hasMin = false;

			foreach (var item in source)
			{
				var value = selector(item);
				if ((!hasMin) || (value.CompareTo(minValue) < 0))
				{
					minItem = item;
					minValue = value;
				}
				hasMin = true;
			}

			if (!hasMin)
				throw new Exception("No elements in list");

			return Tuple.Create(minItem, minValue);
		}

		public static Tuple<TSource, TValue> MaxByTuple<TSource, TValue>(this IEnumerable<TSource> source, Func<TSource, TValue> selector) where TValue : IComparable
		{
			var maxItem = default(TSource);
			var maxValue = default(TValue);
			bool hasMax = false;

			foreach (var item in source)
			{
				var value = selector(item);
				if ((!hasMax) || (value.CompareTo(maxValue) > 0))
				{
					maxItem = item;
					maxValue = value;
				}
				hasMax = true;
			}

			if (!hasMax)
				throw new Exception("No elements in list");

			return Tuple.Create(maxItem, maxValue);
		}

		public static TSource MinBy<TSource, TValue>(this IEnumerable<TSource> source, Func<TSource, TValue> selector) where TValue : IComparable => MinByTuple(source, selector).Item1;
		public static TSource MaxBy<TSource, TValue>(this IEnumerable<TSource> source, Func<TSource, TValue> selector) where TValue : IComparable => MaxByTuple(source, selector).Item1;

		public static IEnumerable<Tuple<TSource, TSource>> WithPrev<TSource>(this IEnumerable<TSource> source)
		{
			var first = true;
			var last = default(TSource);
			foreach (var item in source)
			{
				if (!first)
					yield return Tuple.Create(last, item);

				last = item;
				first = false;
			}
		}

		public static IEnumerable<TSource> SelectMany<TSource>(this IEnumerable<IEnumerable<TSource>> source) => source.SelectMany(items => items);

		public static IEnumerable<TResult> TrySelect<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector, TResult defValue)
		{
			foreach (var value in source)
			{
				var result = defValue;
				try { result = selector(value); } catch { }
				yield return result;
			}
		}

		public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<TKey> keys, IEnumerable<TValue> values)
		{
			var result = new Dictionary<TKey, TValue>();
			//=> keys.Zip(values, (key, value) => new { key, value }).ToDictionary(obj => obj.key, obj => obj.value);
			using (var keyEnumerator = keys.GetEnumerator())
			using (var valueEnumerator = values.GetEnumerator())
			{
				while (true)
				{
					var hasKey = keyEnumerator.MoveNext();
					var hasValue = valueEnumerator.MoveNext();
					if (hasKey != hasValue)
						throw new Exception("Inputs must be the same size");
					if (!hasKey)
						break;
					if (result.ContainsKey(keyEnumerator.Current))
						throw new Exception("Key already in result");
					result[keyEnumerator.Current] = valueEnumerator.Current;
				}
			}
			return result;
		}
	}
}
