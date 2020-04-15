using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Editor.TaskRunning
{
	class FluentTaskRunner<T>
	{
		Transform startTransform;

		public FluentTaskRunner(IEnumerable<object> values)
		{
			var transform = new Transform();
			transform.Runner = _ =>
			{
				if (transform.NextTransform == null)
					return;

				transform.NextTransform.AddTasks(values);
				transform.NextTransform.Run();
			};
			AddTransform(transform);
		}

		public FluentTaskRunner(Transform startTransform)
		{
			this.startTransform = startTransform;
		}

		FluentTaskRunner<TResult> GetTaskRunnerData<TResult>()
		{
			if (typeof(T) == typeof(TResult))
				return (FluentTaskRunner<TResult>)(object)this;
			return new FluentTaskRunner<TResult>(startTransform);
		}

		public FluentTaskRunner<TResult> Select<TResult>(Func<T, TResult> func)
		{
			AddTransform(new Transform { DoTask = (value, progress) => new List<object> { func((T)value) } });
			return GetTaskRunnerData<TResult>();
		}

		public FluentTaskRunner<TResult> Select<TResult>(Func<T, ITaskRunnerProgress, TResult> func)
		{
			AddTransform(new Transform { DoTask = (value, progress) => new List<object> { func((T)value, progress) } });
			return GetTaskRunnerData<TResult>();
		}

		public FluentTaskRunner<TResult> SelectMany<TResult>(Func<T, IEnumerable<TResult>> func)
		{
			AddTransform(new Transform { DoTask = (value, progress) => func((T)value).Cast<object>() });
			return GetTaskRunnerData<TResult>();
		}

		public void ForEach(Action<T> action)
		{
			AddTransform(new Transform { Runner = values => values.ForEach(value => action((T)value)) });
			startTransform.Run();
		}

		public void ParallelForEach(Action<T> action)
		{
			AddTransform(new Transform { Runner = values => values.ForEach(value => TaskRunner.AddTask(progress => action((T)value))) });
			startTransform.Run();
		}

		public void ParallelForEach(Action<T, ITaskRunnerProgress> action)
		{
			AddTransform(new Transform { Runner = values => values.ForEach(value => TaskRunner.AddTask(progress => action((T)value, progress))) });
			startTransform.Run();
		}

		public void ToList(Action<List<T>> action)
		{
			AddTransform(new Transform { Runner = values => action(values.Cast<T>().ToList()) });
			startTransform.Run();
		}

		void AddTransform(Transform transform)
		{
			if (startTransform == null)
			{
				startTransform = transform;
				return;
			}

			var next = startTransform;
			while (next.NextTransform != null)
				next = next.NextTransform;
			next.NextTransform = transform;
		}
	}
}
