using System;
using System.Collections.Generic;

namespace NeoEdit.TaskRunning
{
	public class FluentTaskRunner<T>
	{
		Action<TaskRunnerTask> runTask;

		FluentTaskRunner(Action<TaskRunnerTask> runTask) => this.runTask = runTask;

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

		#region ToList
		public IReadOnlyList<T> ToList()
		{
			IReadOnlyList<T> result = default;
			runTask(new TaskRunnerTask<T, bool>(((Func<IReadOnlyList<T>>)ToList).Method, null, null, (items, results) => result = items));
			return result;
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

		#region ForAll
		public void ForAll(Action<T> action, Func<T, long> getSize = null) => ForAll((item, index, progress) => action(item), getSize);
		public void ForAll(Action<T, int> action, Func<T, long> getSize = null) => ForAll((item, index, progress) => action(item, index), getSize);
		public void ForAll(Action<T, int, Action<long>> action, Func<T, long> getSize = null) => runTask(new TaskRunnerTask<T, bool>(action.Method, getSize, (item, index, progress) => { action(item, index, progress); return false; }, null));
		#endregion
	}
}
