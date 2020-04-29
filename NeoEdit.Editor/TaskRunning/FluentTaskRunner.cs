using System;
using System.Collections.Generic;
using NeoEdit.Common;

namespace NeoEdit.Editor.TaskRunning
{
	class FluentTaskRunner<T>
	{
		Action<ITaskRunnerTask> startTask;

		FluentTaskRunner(Action<ITaskRunnerTask> startTask) => this.startTask = startTask;

		public FluentTaskRunner(IEnumerable<T> items) => startTask = nextTask => nextTask.Run(items);

		#region Select
		public FluentTaskRunner<TResult> Select<TResult>(Func<T, TResult> func) => Select((item, index, progress) => func(item));
		public FluentTaskRunner<TResult> Select<TResult>(Func<T, int, TResult> func) => Select((item, index, progress) => func(item, index));
		public FluentTaskRunner<TResult> Select<TResult>(Func<T, int, ITaskRunnerProgress, TResult> func)
		{
			var curStartTask = startTask;
			startTask = nextTask => curStartTask(new TaskRunnerTask<T, TResult>(func, (items, results) => nextTask.Run(results)));
			return new FluentTaskRunner<TResult>(startTask);
		}
		#endregion

		#region Where
		public FluentTaskRunner<T> Where(Func<T, bool> predicate) => Where((item, index, progress) => predicate(item));
		public FluentTaskRunner<T> Where(Func<T, int, bool> predicate) => Where((item, index, progress) => predicate(item, index));
		public FluentTaskRunner<T> Where(Func<T, int, ITaskRunnerProgress, bool> predicate)
		{
			var curStartTask = startTask;
			startTask = nextTask => curStartTask(new TaskRunnerTask<T, bool>(predicate, (items, results) =>
			{
				var newList = new List<T>();
				for (var ctr = 0; ctr < items.Count; ++ctr)
					if (results[ctr])
						newList.Add(items[ctr]);
				nextTask.Run(newList);
			}));
			return this;
		}
		#endregion

		#region ToList
		public void ToList(Action<IReadOnlyList<T>> action) => startTask(new TaskRunnerTask<T, T>(null, (items, results) => action(results)));
		#endregion

		#region ParallelForEach
		public void ParallelForEach(Action<T> action) => ParallelForEach((item, index, progress) => action(item));
		public void ParallelForEach(Action<T, int> action) => ParallelForEach((item, index, progress) => action(item, index));
		public void ParallelForEach(Action<T, int, ITaskRunnerProgress> action) => startTask(new TaskRunnerTask<T, T>((item, index, progress) => { action(item, index, progress); return default; }, null));
		#endregion

		#region SequentialForEach
		public void SequentialForEach(Action<T> action) => startTask(new TaskRunnerTask<T, T>(null, (items, results) => results.ForEach(item => action(item))));
		#endregion
	}
}
