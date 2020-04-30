using System;
using System.Collections.Generic;
using NeoEdit.Common;

namespace NeoEdit.Editor.TaskRunning
{
	class FluentTaskRunner<T>
	{
		Action<ITaskRunnerTask> startTask;

		FluentTaskRunner(Action<ITaskRunnerTask> startTask) => this.startTask = startTask;

		public FluentTaskRunner(IEnumerable<T> items) => startTask = nextTask => nextTask.Start(items);

		#region Select
		public FluentTaskRunner<TResult> Select<TResult>(Func<T, TResult> func, Func<T, long> getSize = null) => Select((item, index, progress) => func(item), getSize);
		public FluentTaskRunner<TResult> Select<TResult>(Func<T, int, TResult> func, Func<T, long> getSize = null) => Select((item, index, progress) => func(item, index), getSize);
		public FluentTaskRunner<TResult> Select<TResult>(Func<T, int, ITaskRunnerProgress, TResult> func, Func<T, long> getSize = null)
		{
			var curStartTask = startTask;
			startTask = nextTask => curStartTask(new TaskRunnerTask<T, TResult>(getSize, func, (items, results) => nextTask.Start(results)));
			return new FluentTaskRunner<TResult>(startTask);
		}
		#endregion

		#region Where
		public FluentTaskRunner<T> Where(Func<T, bool> predicate, Func<T, long> getSize = null) => Where((item, index, progress) => predicate(item), getSize);
		public FluentTaskRunner<T> Where(Func<T, int, bool> predicate, Func<T, long> getSize = null) => Where((item, index, progress) => predicate(item, index), getSize);
		public FluentTaskRunner<T> Where(Func<T, int, ITaskRunnerProgress, bool> predicate, Func<T, long> getSize = null)
		{
			var curStartTask = startTask;
			startTask = nextTask => curStartTask(new TaskRunnerTask<T, bool>(getSize, predicate, (items, results) =>
			{
				var newList = new List<T>();
				for (var ctr = 0; ctr < items.Count; ++ctr)
					if (results[ctr])
						newList.Add(items[ctr]);
				nextTask.Start(newList);
			}));
			return this;
		}
		#endregion

		#region ToList
		public void ToList(Action<IReadOnlyList<T>> action, Func<T, long> getSize = null) => startTask(new TaskRunnerTask<T, T>(getSize, null, (items, results) => action(items)));
		#endregion

		#region ParallelForEach
		public void ParallelForEach(Action<T> action, Func<T, long> getSize = null) => ParallelForEach((item, index, progress) => action(item), getSize);
		public void ParallelForEach(Action<T, int> action, Func<T, long> getSize = null) => ParallelForEach((item, index, progress) => action(item, index), getSize);
		public void ParallelForEach(Action<T, int, ITaskRunnerProgress> action, Func<T, long> getSize = null) => startTask(new TaskRunnerTask<T, T>(getSize, (item, index, progress) => { action(item, index, progress); return default; }, null));
		#endregion
	}
}
