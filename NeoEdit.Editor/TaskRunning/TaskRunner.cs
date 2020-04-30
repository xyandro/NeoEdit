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
		static readonly int NumThreads = Environment.ProcessorCount; // 0 will run everything in calling thread
		const int DefaultTaskTotal = 1;
		const int ForceCancelDelay = 5000;

		public static FluentTaskRunner<T> AsTaskRunner<T>(this IEnumerable<T> items) => new FluentTaskRunner<T>(items);
		public static FluentTaskRunner<int> Range(int start, int count) => new FluentTaskRunner<int>(Enumerable.Range(start, count));
		public static FluentTaskRunner<T> Repeat<T>(T value, int count) => new FluentTaskRunner<T>(Enumerable.Repeat(value, count));
		public static void Run(Action action) => new FluentTaskRunner<int>(new List<int> { 0 }).ParallelForEach(item => action());
		public static void Run(Action<ITaskRunnerProgress> action) => new FluentTaskRunner<int>(new List<int> { 0 }).ParallelForEach((item, index, progress) => action(progress));

		static readonly ManualResetEvent finished = new ManualResetEvent(true);
		static readonly ManualResetEvent workReady = new ManualResetEvent(false);
		static readonly Stack<ITaskRunnerTask> tasks = new Stack<ITaskRunnerTask>();
		static ITaskRunnerTask activeTask = null;
		static long current = 0, total = 0;
		static int running = 0;
		static Exception exception;
		static readonly List<Thread> threads = new List<Thread>();

		static TaskRunner() => Start();

		static void Start()
		{
			threads.AddRange(Enumerable.Range(1, NumThreads).Select(num => new Thread(TaskRunnerThread) { Name = $"{nameof(TaskRunner)} {num}" }));
			threads.ForEach(thread => thread.Start());
		}

		static ITaskRunnerProgress progress = NumThreads == 0 ? CreateTaskRunnerProgress() : null;
		public static void AddTask(ITaskRunnerTask task)
		{
			lock (tasks)
			{
				if (exception != null)
				{
					ExceptionDispatchInfo.Capture(exception).Throw();
					throw exception;
				}

				if (NumThreads == 0)
				{
					task.Run(progress);
					return;
				}

				total += task.ItemCount * DefaultTaskTotal;
				tasks.Push(task);
				activeTask = task;
				finished.Reset();
				workReady.Set();
			}
		}

		public static void ForceCancel()
		{
			lock (tasks)
			{
				if (finished.WaitOne(0))
					return;
				exception = new Exception($"All active tasks killed. Program may be unstable as a result of this operation.");
			}

			threads.ForEach(thread => thread.Abort());
			var endWait = DateTime.Now.AddMilliseconds(ForceCancelDelay);
			foreach (var thread in threads)
				if (!thread.Join(Math.Max(0, (int)(endWait - DateTime.Now).TotalMilliseconds)))
					throw new Exception("Attempt to abort failed. Everything's probably broken now.");

			finished.Set();
			workReady.Reset();
			tasks.Clear();
			threads.Clear();
			current = total = running = 0;

			Start();
		}

		public static bool Cancel(Exception ex = null)
		{
			if (finished.WaitOne(0))
				return false;

			lock (tasks)
			{
				exception = exception ?? ex ?? new OperationCanceledException();
				workReady.Reset();
				activeTask = null;
				tasks.Clear();
				return true;
			}
		}

		static void RemoveFinishedTasks()
		{
			ITaskRunnerTask task;

			lock (tasks)
			{
				while (true)
				{
					if (tasks.Count == 0)
					{
						activeTask = null;
						workReady.Reset();
						break;
					}

					task = tasks.Peek();
					if (!task.Finished)
					{
						activeTask = task;
						break;
					}

					tasks.Pop();
				}
			}
		}

		static void TaskRunnerThread()
		{
			var progress = CreateTaskRunnerProgress();

			ITaskRunnerTask task;
			while (true)
			{
				task = activeTask;
				if (task == null)
				{
					lock (tasks)
						if (running == 0)
							finished.Set();

					workReady.WaitOne();
					continue;
				}

				if (task.Finished)
				{
					RemoveFinishedTasks();
					continue;
				}

				lock (tasks)
					++running;

				try { task.Run(progress); }
				catch (Exception ex) when (!(ex is ThreadAbortException)) { Cancel(ex); }

				lock (tasks)
					--running;
			}
		}

		static TaskRunnerProgress CreateTaskRunnerProgress()
		{
			long lastCurrent = 0, lastTotal = DefaultTaskTotal;
			void ReportProgress(long newCurrent, long newTotal)
			{
				if (exception != null)
				{
					ExceptionDispatchInfo.Capture(exception).Throw();
					throw exception;
				}

				Interlocked.Add(ref total, newTotal - lastTotal);
				Interlocked.Add(ref current, newCurrent - lastCurrent);

				lastTotal = newTotal;
				lastCurrent = newCurrent;
			}
			var progress = new TaskRunnerProgress { SetProgressAction = ReportProgress };
			return progress;
		}

		public static void WaitForFinish(ITabsWindow tabsWindow)
		{
			lock (tasks)
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

			lock (tasks)
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
