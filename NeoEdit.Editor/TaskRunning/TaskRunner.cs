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
		static readonly int NumThreads = Environment.ProcessorCount; // 0 will run everything in calling thread, 1 will run everything in child thread
		const int DefaultTaskTotal = 1;
		const int ForceCancelDelay = 5000;
		const int MaxTaskGroupSize = 1000;

		public static FluentTaskRunner<T> AsTaskRunner<T>(this IEnumerable<T> items) => new FluentTaskRunner<T>(items);
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
					RunSynchronously(task);
					return;
				}

				total += task.ItemCount * DefaultTaskTotal;
				tasks.Push(task);
				activeTask = task;
				finished.Reset();
				workReady.Set();
			}
		}

		static ITaskRunnerProgress progress = NumThreads == 0 ? new TaskRunnerProgress() : null;
		static void RunSynchronously(ITaskRunnerTask task)
		{
			for (var index = 0; index < task.ItemCount; ++index)
				task.RunFunc(index, progress);
			task.RunDone();
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

				Interlocked.Add(ref total, newTotal - lastTotal);
				Interlocked.Add(ref current, newCurrent - lastCurrent);

				lastTotal = newTotal;
				lastCurrent = newCurrent;
			}
			var progress = new TaskRunnerProgress { SetProgressAction = ReportProgress };

			while (true)
			{
				workReady.WaitOne();

				lock (tasks)
					++running;

				try
				{
					while (true)
					{
						var task = activeTask;
						if (task == null)
							break;

						var taskGroupSize = task.Waiting >> 6;
						if (taskGroupSize > MaxTaskGroupSize)
							taskGroupSize = MaxTaskGroupSize;
						if (taskGroupSize < 1)
							taskGroupSize = 1;

						var index = task.AddNextIndex(taskGroupSize);
						var endIndex = index + taskGroupSize;
						if (index > task.ItemCount)
							index = task.ItemCount;
						if (endIndex >= task.ItemCount)
						{
							endIndex = task.ItemCount;
							taskGroupSize = endIndex - index;
							lock (tasks)
							{
								if ((tasks.Count != 0) && (tasks.Peek() == task))
								{
									++taskGroupSize;
									tasks.Pop();
									if (tasks.Count == 0)
									{
										activeTask = null;
										workReady.Reset();
									}
									else
										activeTask = tasks.Peek();
								}
							}
						}

						for (; index < endIndex; ++index)
						{
							lastCurrent = 0;
							lastTotal = DefaultTaskTotal;

							task.RunFunc(index, progress);

							ReportProgress(lastTotal, lastTotal);
						}

						if ((taskGroupSize != 0) && (task.AddWaiting(-taskGroupSize) == -1))
							task.RunDone();
					}
				}
				catch (Exception ex) when (!(ex is ThreadAbortException)) { Cancel(ex); }

				lock (tasks)
				{
					--running;
					if ((running == 0) && (tasks.Count == 0))
						finished.Set();
				}
			}
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
