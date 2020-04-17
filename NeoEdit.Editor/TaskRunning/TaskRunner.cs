using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using NeoEdit.Common;

namespace NeoEdit.Editor.TaskRunning
{
	static class TaskRunner
	{
		const int DefaultTaskTotal = 1;
		const int ResetWait = 5000;

		static readonly int NumThreads = Environment.ProcessorCount;

		public static FluentTaskRunner<T> AsTaskRunner<T>(this IReadOnlyList<T> list) => new FluentTaskRunner<T>(list, list.Count);
		public static FluentTaskRunner<T> Create<T>(this IEnumerable<T> list, int count) => new FluentTaskRunner<T>(list, count);
		public static void Run(Action action) => new FluentTaskRunner<int>(new List<int> { 0 }, 1).ParallelForEach(obj => action());
		public static void Run(Action<ITaskRunnerProgress> action) => new FluentTaskRunner<int>(new List<int> { 0 }, 1).ParallelForEach((obj, progress) => action(progress));
		public static FluentTaskRunner<int> Range(int start, int count) => Create(Enumerable.Range(start, count), count);
		public static FluentTaskRunner<T> Repeat<T>(T item, int count) => Create(Enumerable.Repeat(item, count), count);

		static object lockObj = new object();
		static readonly ManualResetEvent finished = new ManualResetEvent(true);
		static Semaphore semaphore;
		static readonly Queue<TaskRunnerData> tasks = new Queue<TaskRunnerData>();
		static long current = 0, total = 0;
		static int running = 0;
		static Exception exception;

		static List<Thread> threads;

		static TaskRunner() => Start();

		static void Start()
		{
			semaphore = new Semaphore(0, int.MaxValue);

			threads = Enumerable.Range(1, NumThreads).ForEach(x => new Thread(TaskRunnerThread) { Name = $"TaskRunner {x}" }).ToList();
			threads.ForEach(thread => thread.Start());
		}

		internal static void Add(TaskRunnerData data)
		{
			if (data.Count == 0)
				return;

			lock (lockObj)
			{
				if (exception != null)
				{
					ExceptionDispatchInfo.Capture(exception).Throw();
					throw exception;
				}

				finished.Reset();
				total += data.Count * DefaultTaskTotal;
				tasks.Enqueue(data);
				semaphore.Release(data.Count);
			}
		}

		public static void ForceCancel()
		{
			lock (lockObj)
			{
				if (finished.WaitOne(0))
					return;
				exception = new Exception($"All active tasks killed. Program may be unstable as a result of this operation.");
			}

			threads.ForEach(thread => thread.Abort());
			var endWait = DateTime.Now.AddMilliseconds(ResetWait);
			foreach (var thread in threads)
				if (!thread.Join(Math.Max(0, (int)(endWait - DateTime.Now).TotalMilliseconds)))
					throw new Exception("Attempt to abort failed. Everything's probably broken now.");

			lock (lockObj)
			{
				finished.Set();
				semaphore.Dispose();
				semaphore = null;
				tasks.Clear();
				threads = null;
				current = total = running = 0;

				Start();
			}
		}

		public static bool Cancel(Exception ex = null)
		{
			lock (lockObj)
			{
				if (finished.WaitOne(0))
					return false;
				if (exception == null)
					exception = ex ?? new OperationCanceledException();
				return true;
			}
		}

		static void TaskRunnerThread()
		{
			long lastCurrent = 0, lastTotal = DefaultTaskTotal;
			void ReportProgress(long newCurrent, long newTotal)
			{
				if (exception != null)
				{
					ExceptionDispatchInfo.Capture(exception).Throw();
					throw exception;
				}

				lock (lockObj)
				{
					current += newCurrent - lastCurrent;
					lastCurrent = newCurrent;

					total += newTotal - lastTotal;
					lastTotal = newTotal;
				}
			}
			var progress = new TaskRunnerProgress { SetProgressAction = ReportProgress };

			while (true)
			{
				semaphore.WaitOne();

				TaskRunnerData task;
				object obj = null;
				int index;

				lock (lockObj)
				{
					task = tasks.Peek();

					if (exception == null)
					{
						if (task.Enumerator.MoveNext())
							obj = task.Enumerator.Current;
						else
							Cancel(new Exception("Too few items in enumerable"));
					}

					index = task.NextIndex++;
					if (task.NextIndex == task.Count)
					{
						if ((exception == null) && (task.Enumerator.MoveNext()))
							Cancel(new Exception("Too many items in enumerable"));
						task.Enumerator.Dispose();
						tasks.Dequeue();
					}

					++running;
				}

				if (exception == null)
				{
					try
					{
						lastCurrent = 0;
						lastTotal = DefaultTaskTotal;

						var result = obj;
						if (task.Func != null)
							result = task.Func(obj, progress);
						task.Action?.Invoke(index, result, progress);

						ReportProgress(lastTotal, lastTotal);
					}
					catch (Exception ex) when (!(ex is ThreadAbortException)) { Cancel(ex); }
				}

				lock (lockObj)
				{
					--running;
					if ((running == 0) && (tasks.Count == 0))
						finished.Set();
				}
			}
		}

		public static void WaitForFinish(ITabsWindow tabsWindow)
		{
			lock (lockObj)
				if (finished.WaitOne(0))
					return;

			tabsWindow.SetTaskRunnerProgress(0);
			while (true)
			{
				if (finished.WaitOne(100))
					break;
				tabsWindow.SetTaskRunnerProgress((double)current / total);
			}
			tabsWindow.SetTaskRunnerProgress(null);

			lock (lockObj)
			{
				var ex = exception;

				exception = null;
				current = total = 0;

				if (ex != null)
				{
					ExceptionDispatchInfo.Capture(ex).Throw();
					throw ex;
				}
			}
		}
	}
}
