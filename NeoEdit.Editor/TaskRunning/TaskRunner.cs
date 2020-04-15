using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NeoEdit.Common;

namespace NeoEdit.Editor.TaskRunning
{
	static class TaskRunner
	{
		const int DefaultTaskTotal = 1;
		static readonly int NumThreads = Environment.ProcessorCount;

		public static FluentTaskRunner<T> AsTaskRunner<T>(this IEnumerable<T> items) => new FluentTaskRunner<T>(items.Cast<object>().ToList());

		static TaskRunner() => Enumerable.Range(0, NumThreads).ForEach(x => new Thread(TaskRunnerThread).Start());

		static readonly ManualResetEvent finished = new ManualResetEvent(true);
		static readonly Semaphore semaphore = new Semaphore(0, int.MaxValue);
		static readonly Queue<Action<ITaskRunnerProgress>> tasks = new Queue<Action<ITaskRunnerProgress>>();
		static long current = 0, total = 0;
		static int running = -1;
		static Exception exception;

		public static bool Cancel(Exception ex = null)
		{
			lock (semaphore)
			{
				if (finished.WaitOne(0))
					return false;
				if (exception == null)
					exception = ex ?? new OperationCanceledException();
				return true;
			}
		}

		public static void AddTask(Action task) => AddTask(progress => task());

		public static void AddTask(Action<ITaskRunnerProgress> task)
		{
			lock (semaphore)
			{
				if (exception != null)
					throw exception;
				finished.Reset();
				tasks.Enqueue(task);
				lock (finished)
					total += DefaultTaskTotal;
				if (running != -1)
					semaphore.Release();
			}
		}

		static void TaskRunnerThread()
		{
			long lastCurrent = 0, lastTotal = DefaultTaskTotal;
			void ReportProgress(long newCurrent, long newTotal)
			{
				if (exception != null)
					throw exception;

				lock (finished)
				{
					current += newCurrent - lastCurrent;
					total += newTotal - lastTotal;
					lastCurrent = newCurrent;
					lastTotal = newTotal;
				}
			}
			var progress = new TaskRunnerProgress { SetProgressAction = ReportProgress };

			while (true)
			{
				semaphore.WaitOne();

				Action<ITaskRunnerProgress> task;
				lock (semaphore)
				{
					task = tasks.Dequeue();
					++running;
				}

				lastCurrent = 0;
				lastTotal = DefaultTaskTotal;
				if (exception == null)
				{
					try
					{
						task(progress);
						ReportProgress(lastTotal, lastTotal);
					}
					catch (Exception ex) { Cancel(ex); }
				}

				lock (semaphore)
				{
					--running;
					if ((tasks.Count == 0) && (running == 0))
						finished.Set();
				}
			}
		}

		public static void WaitForFinish(ITabsWindow tabsWindow)
		{
			lock (semaphore)
			{
				if (tasks.Count == 0)
					return;
				running = 0;
				semaphore.Release(tasks.Count);
			}

			tabsWindow.SetTaskRunnerProgress(0);
			while (true)
			{
				if (finished.WaitOne(100))
					break;
				tabsWindow.SetTaskRunnerProgress((double)current / total);
			}
			tabsWindow.SetTaskRunnerProgress(null);

			lock (semaphore)
			{
				var ex = exception;

				running = -1;
				exception = null;
				current = total = 0;

				if (ex != null)
					throw ex;
			}
		}
	}
}
