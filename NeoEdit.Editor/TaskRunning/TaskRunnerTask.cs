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
		bool Finished { get; set; }
		long Total { get; }

		void Start(IEnumerable itemsEnum);
		void Run(TaskRunnerProgress progress);
	}

	class TaskRunnerTask<TSource, TResult> : ITaskRunnerTask
	{
		const int MaxGroupSize = 1000;

		public int ItemCount => items.Count;
		public bool Finished { get; set; } = false;
		public long Total { get; private set; } = 0;

		IReadOnlyList<TSource> items;
		IReadOnlyList<long> sizes;
		List<TResult> results;
		readonly Func<TSource, long> getSize;
		readonly Func<TSource, int, ITaskRunnerProgress, TResult> func;
		readonly Action<IReadOnlyList<TSource>, IReadOnlyList<TResult>> done;
		int nextIndex = 0;
		int waiting = 0;

		public TaskRunnerTask(Func<TSource, long> getSize, Func<TSource, int, ITaskRunnerProgress, TResult> func, Action<IReadOnlyList<TSource>, IReadOnlyList<TResult>> done)
		{
			this.getSize = getSize;
			this.func = func;
			this.done = done;
		}

		public void Start(IEnumerable itemsEnum)
		{
			if (!(itemsEnum is IReadOnlyList<TSource> items))
				items = itemsEnum.Cast<TSource>().ToArray();

			this.items = items;

			if (getSize == null)
				Total += items.Count * 100;
			else
			{
				var sizes = new List<long>(items.Count);
				foreach (var item in items)
				{
					var size = getSize.Invoke(item);
					Total += size;
					sizes.Add(size);
				}
				this.sizes = sizes;
			}

			results = new List<TResult>(items.Count);
			for (var ctr = 0; ctr < items.Count; ++ctr)
				results.Add(default);

			waiting = items.Count;

			TaskRunner.AddTask(this);
		}

		public void Run(TaskRunnerProgress progress)
		{
			if ((func == null) || (items.Count == 0))
			{
				Finished = true;
				if (Interlocked.Add(ref nextIndex, 1) == 1) // Only call once
				{
					progress.Reset(Total);
					done?.Invoke(items, results);
					progress.Current = Total;
				}
				return;
			}

			int count, index, endIndex;
			while (true)
			{
				count = waiting >> 6;
				if (count > MaxGroupSize)
					count = MaxGroupSize;
				if (count < 1)
					count = 1;
				index = Interlocked.Add(ref nextIndex, count) - count;

				// Break if we're beyond the end of the batch
				if (index >= items.Count)
					break;

				endIndex = index + count;
				// If this group includes the end of the batch, mark it as finished so no other tasks try to get more
				if (endIndex >= items.Count)
				{
					Finished = true;
					endIndex = items.Count;
					count = endIndex - index;
				}

				while (index < endIndex)
				{
					progress.Reset(sizes?[index] ?? 100);
					results[index] = func(items[index], index, progress);
					progress.Current = progress.Total;
					++index;
				}

				if (Interlocked.Add(ref waiting, -count) == 0)
				{
					done?.Invoke(items, results);
					break;
				}
			}
		}
	}
}
