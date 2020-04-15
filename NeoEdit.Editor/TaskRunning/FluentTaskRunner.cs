using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Editor.TaskRunning
{
	class FluentTaskRunner<T>
	{
		TaskRunnerData data;

		public FluentTaskRunner(IEnumerable<T> items, int count)
		{
			data = new TaskRunnerData
			{
				Enumerator = items.Cast<object>().GetEnumerator(),
				Count = count,
			};
		}

		FluentTaskRunner(TaskRunnerData data) => this.data = data;

		public FluentTaskRunner<TResult> Select<TResult>(Func<T, TResult> func)
		{
			if (data.Func == null)
				data.Func = (obj, progress) => func((T)obj);
			else
				data.Func = (obj, progress) => func((T)data.Func(obj, progress));
			return new FluentTaskRunner<TResult>(data);
		}

		public FluentTaskRunner<TResult> Select<TResult>(Func<T, ITaskRunnerProgress, TResult> func)
		{
			if (data.Func == null)
				data.Func = (obj, progress) => func((T)obj, progress);
			else
				data.Func = (obj, progress) => func((T)data.Func(obj, progress), progress);
			return new FluentTaskRunner<TResult>(data);
		}

		public void ParallelForEach(Action<T> action)
		{
			data.Action = (index, obj, progress) => action((T)obj);
			TaskRunner.Add(data);
		}

		public void ParallelForEach(Action<T, ITaskRunnerProgress> action)
		{
			data.Action = (index, obj, progress) => action((T)obj, progress);
			TaskRunner.Add(data);
		}

		public void SequentialForEach(Action<T> action) => ToList(list => list.ForEach(action));

		public void ToList(Action<List<T>> action)
		{
			var result = new List<T>(data.Count);
			for (var ctr = 0; ctr < data.Count; ++ctr)
				result.Add(default);
			var remaining = result.Count;
			data.Action = (index, obj, progress) =>
			{
				result[index] = (T)obj;

				bool runAction;
				lock (result)
				{
					--remaining;
					runAction = remaining == 0;
				}
				if (runAction)
					action(result);
			};
			TaskRunner.Add(data);
		}
	}
}
