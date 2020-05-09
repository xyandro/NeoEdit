using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.TaskRunning
{
	public class FluentTaskRunnerGroup<TKey, T> : FluentTaskRunner<T>
	{
		public TKey Key { get; }

		internal FluentTaskRunnerGroup(TKey key, IEnumerable<T> items) : base(items) => Key = key;
	}

	public class FluentTaskRunner<T>
	{
		Action<TaskRunnerTask> runTask;

		internal FluentTaskRunner(Action<TaskRunnerTask> runTask) => this.runTask = runTask;

		internal FluentTaskRunner(IEnumerable<T> items) => runTask = nextTask => nextTask.RunTask(items);

		#region Select
		public FluentTaskRunner<TResult> Select<TResult>(Func<T, TResult> func, Func<T, long> getSize = null) => Select((item, index, progress) => func(item), getSize);
		public FluentTaskRunner<TResult> Select<TResult>(Func<T, int, TResult> func, Func<T, long> getSize = null) => Select((item, index, progress) => func(item, index), getSize);
		public FluentTaskRunner<TResult> Select<TResult>(Func<T, int, Action<long>, TResult> func, Func<T, long> getSize = null)
		{
			return new FluentTaskRunner<TResult>(nextTask => runTask(new TaskRunnerTask<T, TResult>(func.Method, getSize, func, (items, results) => nextTask.RunTask(results))));
		}
		#endregion

		#region SelectMany
		public FluentTaskRunner<TResult> SelectMany<TResult>(Func<T, IEnumerable<TResult>> func, Func<T, long> getSize = null) => SelectMany((item, index, progress) => func(item), getSize);
		public FluentTaskRunner<TResult> SelectMany<TResult>(Func<T, int, IEnumerable<TResult>> func, Func<T, long> getSize = null) => SelectMany((item, index, progress) => func(item, index), getSize);
		public FluentTaskRunner<TResult> SelectMany<TResult>(Func<T, int, Action<long>, IEnumerable<TResult>> func, Func<T, long> getSize = null)
		{
			return new FluentTaskRunner<TResult>(nextTask => runTask(new TaskRunnerTask<T, IEnumerable<TResult>>(func.Method, getSize, func, (items, results) =>
			{
				var nextList = new List<TResult>();
				foreach (var result in results)
					nextList.AddRange(result);
				nextTask.RunTask(nextList);
			})));
		}
		#endregion

		#region Batch
		public FluentTaskRunner<IReadOnlyList<T>> Batch(int batchSize)
		{
			return new FluentTaskRunner<IReadOnlyList<T>>(nextTask => runTask(new TaskRunnerTask<T, T>(((Func<int, FluentTaskRunner<IReadOnlyList<T>>>)Batch).Method, null, null, (items, results) =>
			{
				var nextList = new List<IReadOnlyList<T>>();
				List<T> list = default;
				for (var ctr = 0; ctr < items.Count; ++ctr)
				{
					if (list == null)
						list = new List<T>();
					list.Add(items[ctr]);
					if (list.Count == batchSize)
					{
						nextList.Add(list);
						list = null;
					}
				}
				if (list != null)
					throw new Exception("List was not multiple of batch size");
				nextTask.RunTask(nextList);
			})));
		}
		#endregion

		#region GroupBy
		public FluentTaskRunner<FluentTaskRunnerGroup<TKey, T>> GroupBy<TKey>(Func<T, TKey> func, IEqualityComparer<TKey> comparer = null, Func<T, long> getSize = null) => GroupBy((item, index, progress) => func(item), comparer, getSize);
		public FluentTaskRunner<FluentTaskRunnerGroup<TKey, T>> GroupBy<TKey>(Func<T, int, TKey> func, IEqualityComparer<TKey> comparer = null, Func<T, long> getSize = null) => GroupBy((item, index, progress) => func(item, index), comparer, getSize);
		public FluentTaskRunner<FluentTaskRunnerGroup<TKey, T>> GroupBy<TKey>(Func<T, int, Action<long>, TKey> func, IEqualityComparer<TKey> comparer = null, Func<T, long> getSize = null)
		{
			return new FluentTaskRunner<FluentTaskRunnerGroup<TKey, T>>(nextTask => runTask(new TaskRunnerTask<T, TKey>(func.Method, getSize, func, (items, results) =>
			{
				var nextList = new List<FluentTaskRunnerGroup<TKey, T>>();
				var map = new Dictionary<TKey, List<T>>(comparer ?? EqualityComparer<TKey>.Default);
				for (var ctr = 0; ctr < items.Count; ++ctr)
				{
					if (!map.TryGetValue(results[ctr], out var list))
					{
						list = new List<T>();
						nextList.Add(new FluentTaskRunnerGroup<TKey, T>(results[ctr], list));
						map[results[ctr]] = list;
					}
					list.Add(items[ctr]);
				}
				nextTask.RunTask(nextList);
			})));
		}
		#endregion

		#region Where
		public FluentTaskRunner<T> Where(Func<T, bool> predicate, Func<T, long> getSize = null) => Where((item, index, progress) => predicate(item), getSize);
		public FluentTaskRunner<T> Where(Func<T, int, bool> predicate, Func<T, long> getSize = null) => Where((item, index, progress) => predicate(item, index), getSize);
		public FluentTaskRunner<T> Where(Func<T, int, Action<long>, bool> predicate, Func<T, long> getSize = null)
		{
			return new FluentTaskRunner<T>(nextTask => runTask(new TaskRunnerTask<T, bool>(predicate.Method, getSize, predicate, (items, results) =>
			{
				var nextList = new List<T>();
				for (var ctr = 0; ctr < items.Count; ++ctr)
					if (results[ctr])
						nextList.Add(items[ctr]);
				nextTask.RunTask(nextList);
			})));
		}
		#endregion

		#region All
		public bool All(Func<T, bool> predicate, Func<T, long> getSize = null) => All((item, index, progress) => predicate(item), getSize);
		public bool All(Func<T, int, bool> predicate, Func<T, long> getSize = null) => All((item, index, progress) => predicate(item, index), getSize);
		public bool All(Func<T, int, Action<long>, bool> predicate, Func<T, long> getSize = null)
		{
			bool ret = default;
			runTask(new TaskRunnerTask<T, bool>(predicate.Method, getSize, predicate, (items, results) =>
			{
				ret = true;
				foreach (var result in results)
					if (!result)
					{
						ret = false;
						break;
					}
			}));
			return ret;
		}
		#endregion

		#region DistinctBy
		public FluentTaskRunner<T> DistinctBy<TResult>(Func<T, TResult> func, Func<T, long> getSize = null) => DistinctBy((item, index, progress) => func(item), getSize);
		public FluentTaskRunner<T> DistinctBy<TResult>(Func<T, int, TResult> func, Func<T, long> getSize = null) => DistinctBy((item, index, progress) => func(item, index), getSize);
		public FluentTaskRunner<T> DistinctBy<TResult>(Func<T, int, Action<long>, TResult> func, Func<T, long> getSize = null)
		{
			return new FluentTaskRunner<T>(nextTask => runTask(new TaskRunnerTask<T, TResult>(func.Method, getSize, func, (items, results) =>
			{
				var nextList = new List<T>();
				var seen = new HashSet<TResult>();
				for (var ctr = 0; ctr < items.Count; ++ctr)
				{
					if (seen.Contains(results[ctr]))
						continue;
					seen.Add(results[ctr]);
					nextList.Add(items[ctr]);
				}
				nextTask.RunTask(nextList);
			})));
		}
		#endregion

		#region DuplicateBy
		public FluentTaskRunner<T> DuplicateBy<TResult>(Func<T, TResult> func, Func<T, long> getSize = null) => DuplicateBy((item, index, progress) => func(item), getSize);
		public FluentTaskRunner<T> DuplicateBy<TResult>(Func<T, int, TResult> func, Func<T, long> getSize = null) => DuplicateBy((item, index, progress) => func(item, index), getSize);
		public FluentTaskRunner<T> DuplicateBy<TResult>(Func<T, int, Action<long>, TResult> func, Func<T, long> getSize = null)
		{
			return new FluentTaskRunner<T>(nextTask => runTask(new TaskRunnerTask<T, TResult>(func.Method, getSize, func, (items, results) =>
			{
				var nextList = new List<T>();
				var seen = new HashSet<TResult>();
				for (var ctr = 0; ctr < items.Count; ++ctr)
				{
					if (seen.Contains(results[ctr]))
						nextList.Add(items[ctr]);
					else
						seen.Add(results[ctr]);
				}
				nextTask.RunTask(nextList);
			})));
		}
		#endregion

		#region MatchBy
		public FluentTaskRunner<T> MatchBy<TResult>(Func<T, TResult> func, Func<T, long> getSize = null) => MatchBy((item, index, progress) => func(item), getSize);
		public FluentTaskRunner<T> MatchBy<TResult>(Func<T, int, TResult> func, Func<T, long> getSize = null) => MatchBy((item, index, progress) => func(item, index), getSize);
		public FluentTaskRunner<T> MatchBy<TResult>(Func<T, int, Action<long>, TResult> func, Func<T, long> getSize = null)
		{
			return new FluentTaskRunner<T>(nextTask => runTask(new TaskRunnerTask<T, TResult>(func.Method, getSize, func, (items, results) =>
			{
				var nextList = new List<T>();
				var previous = default(TResult);
				for (var ctr = 0; ctr < items.Count; ++ctr)
				{
					if ((ctr != 0) && (Equals(results[ctr], previous)))
						nextList.Add(items[ctr]);
					else
						previous = results[ctr];
				}
				nextTask.RunTask(nextList);
			})));
		}
		#endregion

		#region NonMatchBy
		public FluentTaskRunner<T> NonMatchBy<TResult>(Func<T, TResult> func, Func<T, long> getSize = null) => NonMatchBy((item, index, progress) => func(item), getSize);
		public FluentTaskRunner<T> NonMatchBy<TResult>(Func<T, int, TResult> func, Func<T, long> getSize = null) => NonMatchBy((item, index, progress) => func(item, index), getSize);
		public FluentTaskRunner<T> NonMatchBy<TResult>(Func<T, int, Action<long>, TResult> func, Func<T, long> getSize = null)
		{
			return new FluentTaskRunner<T>(nextTask => runTask(new TaskRunnerTask<T, TResult>(func.Method, getSize, func, (items, results) =>
			{
				var nextList = new List<T>();
				var previous = default(TResult);
				for (var ctr = 0; ctr < items.Count; ++ctr)
				{
					if ((ctr == 0) || (!Equals(results[ctr], previous)))
					{
						nextList.Add(items[ctr]);
						previous = results[ctr];
					}
				}
				nextTask.RunTask(nextList);
			})));
		}
		#endregion

		#region NonNullOrWhiteSpace
		public FluentTaskRunner<T> NonNullOrWhiteSpace(Func<T, string> func, Func<T, long> getSize = null) => NonNullOrWhiteSpace((item, index, progress) => func(item), getSize);
		public FluentTaskRunner<T> NonNullOrWhiteSpace(Func<T, int, string> func, Func<T, long> getSize = null) => NonNullOrWhiteSpace((item, index, progress) => func(item, index), getSize);
		public FluentTaskRunner<T> NonNullOrWhiteSpace(Func<T, int, Action<long>, string> func, Func<T, long> getSize = null)
		{
			return new FluentTaskRunner<T>(nextTask => runTask(new TaskRunnerTask<T, string>(func.Method, getSize, func, (items, results) =>
			{
				var nextList = new List<T>();
				for (var ctr = 0; ctr < items.Count; ++ctr)
					if (!string.IsNullOrWhiteSpace(results[ctr]))
						nextList.Add(items[ctr]);
				nextTask.RunTask(nextList);
			})));
		}
		#endregion

		#region ToDictionary
		public Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(Func<T, TKey> keyFunc, Func<T, TValue> valueFunc, IEqualityComparer<TKey> comparer = null, Func<T, long> getSize = null) => ToDictionary((item, index, progress) => keyFunc(item), (item, index, progress) => valueFunc(item), comparer, getSize);
		public Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(Func<T, int, TKey> keyFunc, Func<T, int, TValue> valueFunc, IEqualityComparer<TKey> comparer = null, Func<T, long> getSize = null) => ToDictionary((item, index, progress) => keyFunc(item, index), (item, index, progress) => valueFunc(item, index), comparer, getSize);
		public Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(Func<T, int, Action<long>, TKey> keyFunc, Func<T, int, Action<long>, TValue> valueFunc, IEqualityComparer<TKey> comparer = null, Func<T, long> getSize = null)
		{
			IReadOnlyList<TKey> keys = default;
			IReadOnlyList<TValue> values = default;
			runTask(new TaskRunnerTask<T, TKey>(keyFunc.Method, getSize, keyFunc, (items, results) => keys = results));
			runTask(new TaskRunnerTask<T, TValue>(valueFunc.Method, getSize, valueFunc, (items, results) => values = results));
			var result = new Dictionary<TKey, TValue>(comparer ?? EqualityComparer<TKey>.Default);
			for (var ctr = 0; ctr < keys.Count; ++ctr)
				result[keys[ctr]] = values[ctr];
			return result;
		}
		#endregion

		#region ToList
		public IReadOnlyList<T> ToList()
		{
			IReadOnlyList<T> result = default;
			runTask(new TaskRunnerTask<T, bool>(((Func<IReadOnlyList<T>>)ToList).Method, null, null, (items, results) => result = items));
			return result;
		}
		#endregion

		#region Count
		public int Count()
		{
			int result = default;
			runTask(new TaskRunnerTask<T, bool>(((Func<int>)Count).Method, null, null, (items, results) => result = items.Count));
			return result;
		}
		#endregion

		#region Distinct
		public FluentTaskRunner<T> Distinct() => new FluentTaskRunner<T>(nextTask => runTask(new TaskRunnerTask<T, T>(((Func<FluentTaskRunner<T>>)Distinct).Method, null, null, (items, results) => nextTask.RunTask(items.Distinct()))));
		#endregion

		#region DefaultIfEmpty
		public FluentTaskRunner<T> DefaultIfEmpty(T value)
		{
			return new FluentTaskRunner<T>(nextTask => runTask(new TaskRunnerTask<T, T>(((Func<T, FluentTaskRunner<T>>)DefaultIfEmpty).Method, null, null, (items, results) =>
			{
				if (items.Count == 0)
					items = new List<T> { value };
				nextTask.RunTask(items);
			})));
		}
		#endregion

		#region Reverse
		public FluentTaskRunner<T> Reverse()
		{
			return new FluentTaskRunner<T>(nextTask => runTask(new TaskRunnerTask<T, T>(((Func<FluentTaskRunner<T>>)Reverse).Method, null, null, (items, results) =>
			{
				var nextList = new List<T>(items.Count);
				for (var ctr = items.Count - 1; ctr >= 0; --ctr)
					nextList.Add(items[ctr]);
				nextTask.RunTask(nextList);
			})));
		}
		#endregion

		#region OrderBy
		public FluentTaskRunner<T> OrderBy<TResult>(Func<T, TResult> func, IComparer<TResult> comparer = null, Func<T, long> getSize = null) => OrderBy((item, index, progress) => func(item), comparer, getSize);
		public FluentTaskRunner<T> OrderBy<TResult>(Func<T, int, TResult> func, IComparer<TResult> comparer = null, Func<T, long> getSize = null) => OrderBy((item, index, progress) => func(item, index), comparer, getSize);
		public FluentTaskRunner<T> OrderBy<TResult>(Func<T, int, Action<long>, TResult> func, IComparer<TResult> comparer = null, Func<T, long> getSize = null)
		{
			return new FluentTaskRunner<T>(nextTask => runTask(new TaskRunnerTask<T, TResult>(func.Method, getSize, func, (items, results) => nextTask.RunTask(items.Zip(results, (item, result) => (item, result)).OrderBy(obj => obj.result, comparer ?? Comparer<TResult>.Default).Select(obj => obj.item).ToList()))));
		}
		#endregion

		#region OrderByDescending
		public FluentTaskRunner<T> OrderByDescending<TResult>(Func<T, TResult> func, IComparer<TResult> comparer, Func<T, long> getSize = null) => OrderByDescending((item, index, progress) => func(item), comparer, getSize);
		public FluentTaskRunner<T> OrderByDescending<TResult>(Func<T, int, TResult> func, IComparer<TResult> comparer, Func<T, long> getSize = null) => OrderByDescending((item, index, progress) => func(item, index), comparer, getSize);
		public FluentTaskRunner<T> OrderByDescending<TResult>(Func<T, int, Action<long>, TResult> func, IComparer<TResult> comparer, Func<T, long> getSize = null)
		{
			return new FluentTaskRunner<T>(nextTask => runTask(new TaskRunnerTask<T, TResult>(func.Method, getSize, func, (items, results) => nextTask.RunTask(items.Zip(results, (item, result) => (item, result)).OrderByDescending(obj => obj.result, comparer ?? Comparer<TResult>.Default).Select(obj => obj.item).ToList()))));
		}
		#endregion

		#region OrderBy
		public FluentTaskRunner<T> OrderBy(IComparer<T> comparer = null)
		{
			return new FluentTaskRunner<T>(nextTask => runTask(new TaskRunnerTask<T, T>(((Func<IComparer<T>, FluentTaskRunner<T>>)OrderBy).Method, null, null, (items, results) => nextTask.RunTask(items.OrderBy(obj => obj, comparer ?? Comparer<T>.Default)))));
		}
		#endregion

		#region OrderByDescending
		public FluentTaskRunner<T> OrderByDescending(IComparer<T> comparer = null)
		{
			return new FluentTaskRunner<T>(nextTask => runTask(new TaskRunnerTask<T, T>(((Func<IComparer<T>, FluentTaskRunner<T>>)OrderByDescending).Method, null, null, (items, results) => nextTask.RunTask(items.OrderByDescending(obj => obj, comparer ?? Comparer<T>.Default)))));
		}
		#endregion

		#region ForEach
		public void ForEach(Action<T> action)
		{
			var results = ToList();
			foreach (var item in results)
				action(item);
		}
		#endregion

		#region First
		public T First()
		{
			T result = default;
			runTask(new TaskRunnerTask<T, T>(((Func<T>)First).Method, null, null, (items, results) =>
			{
				if (items.Count == 0)
					throw new Exception("First called on empty list");
				result = items[0];
			}));
			return result;
		}
		#endregion

		#region Last
		public T Last()
		{
			T result = default;
			runTask(new TaskRunnerTask<T, T>(((Func<T>)Last).Method, null, null, (items, results) =>
			{
				if (items.Count == 0)
					throw new Exception("Last called on empty list");
				result = items[items.Count - 1];
			}));
			return result;
		}
		#endregion

		#region Min
		public TResult Min<TResult>(Func<T, TResult> func, Func<T, long> getSize = null) where TResult : IComparable => Min((item, index, progress) => func(item), getSize);
		public TResult Min<TResult>(Func<T, int, TResult> func, Func<T, long> getSize = null) where TResult : IComparable => Min((item, index, progress) => func(item, index), getSize);
		public TResult Min<TResult>(Func<T, int, Action<long>, TResult> func, Func<T, long> getSize = null) where TResult : IComparable
		{
			TResult result = default;
			runTask(new TaskRunnerTask<T, TResult>(func.Method, getSize, func, (items, results) =>
			{
				if (items.Count == 0)
					throw new Exception("No items in enumerable");
				for (var ctr = 0; ctr < items.Count; ++ctr)
				{
					if ((ctr == 0) || (results[ctr].CompareTo(result) < 0))
						result = results[ctr];
				}
			}));
			return result;
		}
		#endregion

		#region Max
		public TResult Max<TResult>(Func<T, TResult> func, Func<T, long> getSize = null) where TResult : IComparable => Max((item, index, progress) => func(item), getSize);
		public TResult Max<TResult>(Func<T, int, TResult> func, Func<T, long> getSize = null) where TResult : IComparable => Max((item, index, progress) => func(item, index), getSize);
		public TResult Max<TResult>(Func<T, int, Action<long>, TResult> func, Func<T, long> getSize = null) where TResult : IComparable
		{
			TResult result = default;
			runTask(new TaskRunnerTask<T, TResult>(func.Method, getSize, func, (items, results) =>
			{
				if (items.Count == 0)
					throw new Exception("No items in enumerable");
				for (var ctr = 0; ctr < items.Count; ++ctr)
				{
					if ((ctr == 0) || (results[ctr].CompareTo(result) > 0))
						result = results[ctr];
				}
			}));
			return result;
		}
		#endregion

		#region ForAll
		public void ForAll(Action<T> action, Func<T, long> getSize = null) => ForAll((item, index, progress) => action(item), getSize);
		public void ForAll(Action<T, int> action, Func<T, long> getSize = null) => ForAll((item, index, progress) => action(item, index), getSize);
		public void ForAll(Action<T, int, Action<long>> action, Func<T, long> getSize = null) => runTask(new TaskRunnerTask<T, bool>(action.Method, getSize, (item, index, progress) => { action(item, index, progress); return false; }, null));
		#endregion
	}
}
