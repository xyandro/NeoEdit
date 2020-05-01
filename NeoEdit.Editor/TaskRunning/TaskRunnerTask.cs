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
		bool ExecuteDone { get; }
		long Total { get; }
		bool IsEmpty { get; }

		void Start(IEnumerable itemsEnum);
		void RunEmpty();
		void RunBatch(TaskRunnerProgress progress);
	}

	class TaskRunnerTask<TSource, TResult> : ITaskRunnerTask
	{
		const int MaxBatchSize = 1000;

		public int ItemCount => items.Count;
		public bool ExecuteDone { get; set; } = false;
		public long Total { get; private set; } = 0;
		public bool IsEmpty => (execute == null) || (items.Count == 0);

		IReadOnlyList<TSource> items;
		IReadOnlyList<long> sizes;
		List<TResult> results;
		readonly Func<TSource, long> getSize;
		readonly Func<TSource, int, ITaskRunnerProgress, TResult> execute;
		readonly Action<IReadOnlyList<TSource>, IReadOnlyList<TResult>> finish;
		int nextIndex = 0;
		int waiting = 0;

		public TaskRunnerTask(Func<TSource, long> getSize, Func<TSource, int, ITaskRunnerProgress, TResult> execute, Action<IReadOnlyList<TSource>, IReadOnlyList<TResult>> finish)
		{
			this.getSize = getSize;
			this.execute = execute;
			this.finish = finish;
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

		public void RunEmpty()
		{
			ExecuteDone = true;
			finish?.Invoke(items, results);
		}

		public void RunBatch(TaskRunnerProgress progress)
		{
			var count = waiting >> 6;
			if (count > MaxBatchSize)
				count = MaxBatchSize;
			if (count < 1)
				count = 1;
			var index = Interlocked.Add(ref nextIndex, count) - count;

			// Exit if we're beyond the end of the batch
			if (index >= items.Count)
				return;

			var endIndex = index + count;
			// If this group includes the end of the batch, mark it as finished so no other tasks try to get more
			if (endIndex >= items.Count)
			{
				ExecuteDone = true;
				endIndex = items.Count;
				count = endIndex - index;
			}

			while (index < endIndex)
			{
				progress.Reset(sizes?[index] ?? 100);
				results[index] = execute(items[index], index, progress);
				progress.Current = progress.Total;
				++index;
			}

			if (Interlocked.Add(ref waiting, -count) == 0)
				finish?.Invoke(items, results);
		}
	}
}
