using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NeoEdit.Common;

namespace NeoEdit.Editor.TaskRunning
{
	interface ITaskRunnerTask
	{
		int ItemCount { get; }
		int Waiting { get; }

		void RunFunc(int index, ITaskRunnerProgress progress);
		void RunDone();
		void Run(IEnumerable itemsEnum);
		int AddNextIndex(int count);
		int AddWaiting(int count);
	}

	class TaskRunnerTask<TSource, TResult> : ITaskRunnerTask
	{
		public IReadOnlyList<TSource> Items { get; private set; }
		public List<TResult> Results { get; private set; }
		public Func<TSource, int, ITaskRunnerProgress, TResult> Func { get; }
		public Action<IReadOnlyList<TResult>> Done { get; }
		int waiting;
		public int Waiting => waiting;
		int nextIndex;

		public TaskRunnerTask(Func<TSource, int, ITaskRunnerProgress, TResult> func, Action<IReadOnlyList<TResult>> done)
		{
			Func = func;
			Done = done;
		}

		public int ItemCount => Items.Count;

		public void Run(IEnumerable itemsEnum)
		{
			if (!(itemsEnum is IReadOnlyList<TSource> items))
				items = itemsEnum.Cast<TSource>().ToArray();

			Items = items;
			Results = new List<TResult>(items.Count);
			for (var ctr = 0; ctr < items.Count; ++ctr)
				Results.Add(default);
			waiting = items.Count;

			TaskRunner.AddTask(this);
		}

		public void RunFunc(int index, ITaskRunnerProgress progress) => Results[index] = Func(Items[index], index, progress);
		public void RunDone() => Done(Results);

		public int AddNextIndex(int count) => Interlocked.Add(ref nextIndex, count) - count;
		public int AddWaiting(int count) => Interlocked.Add(ref waiting, count);
	}
}
