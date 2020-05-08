using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
		int finishLock;
		int finishCount;

		public TaskRunnerTask(MethodInfo methodInfo, Func<TSource, long> getSize, Func<TSource, int, Action<long>, TResult> execute, Action<IReadOnlyList<TSource>, IReadOnlyList<TResult>> finish) : base(methodInfo)
		{
			this.getSize = getSize;
			this.execute = execute;
			this.finish = finish;
		}

		public override void RunTask(IEnumerable itemsEnum)
		{
			if (!(itemsEnum is IEnumerable<TSource> itemsEnumT))
				itemsEnumT = itemsEnum.Cast<TSource>();
			if (!(itemsEnumT is IReadOnlyList<TSource> items))
				items = itemsEnumT.ToList();

			this.items = items;

			if (execute == null)
			{
				finishCount = 0;
				nextExecute = items.Count;
			}
			else
			{
				finishCount = items.Count;
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

			TaskRunner.RunTask(this);
		}

		public override void Run()
		{
			long lastCurrentSize, totalSize;
			void SetProgress(long currentSize)
			{
				TaskRunner.ThrowIfException();
				var delta = currentSize - lastCurrentSize;
				Interlocked.Add(ref epic.current, delta);
				lastCurrentSize = currentSize;
			}

			var count = (items.Count - nextExecute) >> 6;
			if (count > MaxGroupSize)
				count = MaxGroupSize;
			if (count < 1)
				count = 1;

			var index = Interlocked.Add(ref nextExecute, count) - count;
			var endIndex = index + count;
			if (endIndex >= items.Count)
			{
				if (index >= items.Count)
				{
					if (finishCount == 0)
						Finish();
					return;
				}

				canRun = false;
				endIndex = items.Count;
				count = items.Count - index;
			}

			while (index < endIndex)
			{
				lastCurrentSize = 0;
				totalSize = sizes?[index] ?? 100;
				startTicks = Timer.Ticks;
				results[index] = execute(items[index], index, SetProgress);
				Interlocked.Add(ref epic.ticks, Timer.Ticks - startTicks);
				SetProgress(totalSize);
				++index;
			}

			if (Interlocked.Add(ref finishCount, -count) == 0)
			{
				canRun = true;
				TaskRunner.SetActiveTask(this, true);
			}
		}

		void Finish()
		{
			if (Interlocked.CompareExchange(ref finishLock, 1, 0) != 0)
				return;

			canRun = false;
			finish?.Invoke(items, results);
			finished = true;
			epic.tasks[index] = null;

			TaskRunner.SetActiveTask(this, true);
		}
	}
}
