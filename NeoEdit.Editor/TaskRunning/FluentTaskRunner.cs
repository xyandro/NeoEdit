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
			startTask = nextTask => curStartTask(new TaskRunnerTask<T, TResult>(func, results => nextTask.Run(results)));
			return new FluentTaskRunner<TResult>(startTask);
		}
		#endregion

		#region ToList
		public void ToList(Action<IReadOnlyList<T>> action) => startTask(new TaskRunnerTask<T, T>((item, index, progress) => item, results => action(results)));
		#endregion

		#region ParallelForEach
		public void ParallelForEach(Action<T> action) => startTask(new TaskRunnerTask<T, T>((item, index, progress) => { action(item); return default; }, results => { }));
		public void ParallelForEach(Action<T, int> action) => startTask(new TaskRunnerTask<T, T>((item, index, progress) => { action(item, index); return default; }, results => { }));
		public void ParallelForEach(Action<T, int, ITaskRunnerProgress> action) => startTask(new TaskRunnerTask<T, T>((item, index, progress) => { action(item, index, progress); return default; }, results => { }));
		#endregion

		#region SequentialForEach
		public void SequentialForEach(Action<T> action) => startTask(new TaskRunnerTask<T, T>((item, index, progress) => item, results => results.ForEach(item => action(item))));
		#endregion
	}
}
