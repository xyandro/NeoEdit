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
			return new FluentTaskRunner<TResult>(nextTask => runTask(new TaskRunnerTask<T, TResult>("Select", func.Method, getSize, func, (IReadOnlyList<T> items, IReadOnlyList<TResult> results) => nextTask.RunTask(results))));
		}
		#endregion

		#region Where
		public FluentTaskRunner<T> Where(Func<T, bool> predicate, Func<T, long> getSize = null) => Where((item, index, progress) => predicate(item), getSize);
		public FluentTaskRunner<T> Where(Func<T, int, bool> predicate, Func<T, long> getSize = null) => Where((item, index, progress) => predicate(item, index), getSize);
		public FluentTaskRunner<T> Where(Func<T, int, Action<long>, bool> predicate, Func<T, long> getSize = null)
		{
			return new FluentTaskRunner<T>(nextTask => runTask(new TaskRunnerTask<T, bool>("Where", ((Func<Func<T, int, Action<long>, bool>, Func<T, long>, FluentTaskRunner<T>>)Where).Method, getSize, predicate, (items, results) =>
			{
				var nextList = new List<T>();
				for (var ctr = 0; ctr < items.Count; ++ctr)
					if (results[ctr])
						nextList.Add(items[ctr]);
				nextTask.RunTask(nextList);
			})));
		}
		#endregion

		#region ToList
		public IReadOnlyList<T> ToList()
		{
			IReadOnlyList<T> result = default;
			runTask(new TaskRunnerTask<T, bool>("ToList", ((Func<IReadOnlyList<T>>)ToList).Method, null, null, (items, results) => result = items));
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
		public void ForAll(Action<T, int, Action<long>> action, Func<T, long> getSize = null) => runTask(new TaskRunnerTask<T, bool>("ForAll", ((Action<Action<T, int, Action<long>>, Func<T, long>>)ForAll).Method, getSize, (item, index, progress) => { action(item, index, progress); return false; }, null));
		#endregion
	}
}
