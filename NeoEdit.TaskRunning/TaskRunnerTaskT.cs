using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace NeoEdit.TaskRunning
{
	class TaskRunnerTask<TSource, TResult> : TaskRunnerTask
	{
		const int MaxGroupSize = 1000;

		readonly Func<TSource, long> getSize;
		readonly Func<TSource, int, Action<long>, TResult> execute;
		readonly Action<IReadOnlyList<TSource>, IReadOnlyList<TResult>> finish;

		IReadOnlyList<TSource> items;
		TResult[] results;
		IReadOnlyList<long> sizes;
		int nextExecute;
		int finishCount;

		public TaskRunnerTask(Func<TSource, long> getSize, Func<TSource, int, Action<long>, TResult> execute, Action<IReadOnlyList<TSource>, IReadOnlyList<TResult>> finish)
		{
			this.getSize = getSize;
			this.execute = execute;
			this.finish = finish;
		}

		public override void RunTask(IEnumerable itemsEnum, Action<double> idleAction = null)
		{
			if (!(itemsEnum is IEnumerable<TSource> itemsEnumT))
				itemsEnumT = itemsEnum.Cast<TSource>();
			if (!(itemsEnumT is IReadOnlyList<TSource> items))
				items = itemsEnumT.ToList();

			this.items = items;

			if (execute == null)
			{
				finishCount = 1;
				nextExecute = items.Count;
			}
			else
			{
				finishCount = items.Count + 1;
				results = new TResult[items.Count];

				if (getSize == null)
					totalSize = items.Count * 100;
				else
				{
					var sizes = new long[items.Count];
					for (var ctr = 0; ctr < items.Count; ++ctr)
					{
						var size = getSize(items[ctr]);
						sizes[ctr] = size;
						totalSize += size;
					}
					this.sizes = sizes;
				}
			}

			TaskRunner.RunTask(this, idleAction);
		}

		public override void Run()
		{
			long lastCurrentSize, totalSize;
			void SetProgress(long currentSize)
			{
				epic.ThrowIfException();

				Interlocked.Add(ref epic.current, currentSize - lastCurrentSize);
				lastCurrentSize = currentSize;
			}

			var count = (items.Count - nextExecute) >> 6;
			if (count > MaxGroupSize)
				count = MaxGroupSize;
			if (count < 1)
				count = 1;
			if (epic.exception != null)
				count = finishCount;

			var index = Interlocked.Add(ref nextExecute, count) - count;
			if (index > items.Count)
				return;

			var endIndex = index + count;
			if (endIndex > items.Count)
			{
				canRun = false;
				endIndex = items.Count;
				count = items.Count + 1 - index;
			}

			if (epic.exception == null)
			{
				try
				{
					while (index < endIndex)
					{
						lastCurrentSize = 0;
						totalSize = sizes?[index] ?? 100;
						results[index] = execute(items[index], index, SetProgress);
						SetProgress(totalSize);
						++index;
					}
				}
				catch (Exception ex) when (!(ex is ThreadAbortException)) { epic.Cancel(ex); }
			}

			if (Interlocked.Add(ref finishCount, -count) == 0)
			{
				if (epic.exception == null)
				{
					try { finish?.Invoke(items, results); }
					catch (Exception ex) when (!(ex is ThreadAbortException)) { epic.Cancel(ex); }
				}
				finished = true;
				epic.SetFinished(this);
			}
		}
	}
}
