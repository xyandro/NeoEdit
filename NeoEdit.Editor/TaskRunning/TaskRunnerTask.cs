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

		void RunFunc(int index, ITaskRunnerProgress progress);
		void RunDone();
		void Run(IEnumerable itemsEnum);
		void GetGroup(out int index, out int count);
		int SetFinished(int count);
	}

	class TaskRunnerTask<TSource, TResult> : ITaskRunnerTask
	{
		const int MaxGroupSize = 1000;

		public IReadOnlyList<TSource> Items { get; private set; }
		public List<TResult> Results { get; private set; }
		public Func<TSource, int, ITaskRunnerProgress, TResult> Func { get; }
		public Action<IReadOnlyList<TSource>, IReadOnlyList<TResult>> Done { get; }
		int waiting = 0;
		int nextIndex = 0;
		bool fullGroup = false;

		public TaskRunnerTask(Func<TSource, int, ITaskRunnerProgress, TResult> func, Action<IReadOnlyList<TSource>, IReadOnlyList<TResult>> done)
		{
			Func = func;
			Done = done;

			if (Func == null)
			{
				fullGroup = true;
				Func = (item, index, progress) => (TResult)(object)item;
			}
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
		public void RunDone() => Done?.Invoke(Items, Results);

		public void GetGroup(out int index, out int count)
		{
			if (fullGroup)
				count = Items.Count;
			else
			{
				count = waiting >> 6;
				if (count > MaxGroupSize)
					count = MaxGroupSize;
				if (count < 1)
					count = 1;
			}

			index = Interlocked.Add(ref nextIndex, count) - count;
			if (index > Items.Count)
				index = Items.Count;
			if (index + count > Items.Count)
				count = Items.Count - index;
		}

		public int SetFinished(int count) => Interlocked.Add(ref waiting, count);
	}
}
