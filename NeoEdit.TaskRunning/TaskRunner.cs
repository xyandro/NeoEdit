using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace NeoEdit.TaskRunning
{
	public static class TaskRunner
	{
		static readonly int NumThreads = Environment.ProcessorCount; // 0 will run everything in calling thread
		const int ForceCancelDelay = 5000;

		public static FluentTaskRunner<T> AsTaskRunner<T>(this IEnumerable<T> items) => new FluentTaskRunner<T>(items);
		public static FluentTaskRunner<int> Range(int start, int count) => new FluentTaskRunner<int>(Enumerable.Range(start, count));
		public static FluentTaskRunner<T> Repeat<T>(T value, int count) => new FluentTaskRunner<T>(Enumerable.Repeat(value, count));
		public static void Run(Action action) => new FluentTaskRunner<int>(new List<int> { 0 }).ParallelForEach(item => action());
		public static void Run(Action<ITaskRunnerProgress> action) => new FluentTaskRunner<int>(new List<int> { 0 }).ParallelForEach((item, index, progress) => action(progress));

		static readonly ManualResetEvent finished = new ManualResetEvent(true);
		static readonly ManualResetEvent workReady = new ManualResetEvent(false);
		static readonly Stack<ITaskRunnerTask> tasks = new Stack<ITaskRunnerTask>();
		static int updateTasksLock = 0, running = 0;
		static ITaskRunnerTask activeTask = null;
		static long current = 0, total = 0;
		static Exception exception;
		static readonly List<Thread> threads = new List<Thread>();

		static TaskRunner() => Start();

		static void Start()
		{
			threads.AddRange(Enumerable.Range(1, NumThreads).Select(num => new Thread(TaskRunnerThread) { Name = $"{nameof(TaskRunner)} {num}" }));
			threads.ForEach(thread => thread.Start());
		}

		static void ThrowIfException(Exception ex)
		{
			if (ex != null)
				ExceptionDispatchInfo.Capture(ex).Throw();
		}

		static TaskRunnerProgress CreateTaskRunnerProgress()
		{
			return new TaskRunnerProgress(delta =>
			{
				ThrowIfException(exception);
				Interlocked.Add(ref current, delta);
			}, delta => Interlocked.Add(ref total, delta));
		}

		static TaskRunnerProgress progress = NumThreads == 0 ? CreateTaskRunnerProgress() : null;
		internal static void AddTask(ITaskRunnerTask task)
		{
			ThrowIfException(exception);

			if (NumThreads == 0)
			{
				while (!task.ExecuteDone)
					task.RunBatch(progress);
			}

			finished.Reset();
			Interlocked.Add(ref total, task.Total);

			if (task.IsEmpty)
			{
				AddRunning(1);
				task.RunEmpty();
				AddRunning(-1);
				return;
			}

			while (true)
			{
				if (Interlocked.CompareExchange(ref updateTasksLock, 1, 0) != 0)
					continue;

				tasks.Push(task);
				activeTask = task;
				workReady.Set();

				updateTasksLock = 0;
				break;
			}
		}

		static void ClearAllWork()
		{
			workReady.Reset();
			activeTask = null;
			tasks.Clear();
		}

		public static void ForceCancel()
		{
			if (finished.WaitOne(0))
				return;
			exception = new Exception($"All active tasks killed. Program may be unstable as a result of this operation.");

			threads.ForEach(thread => thread.Abort());
			var endWait = DateTime.Now.AddMilliseconds(ForceCancelDelay);
			foreach (var thread in threads)
				if (!thread.Join(Math.Max(0, (int)(endWait - DateTime.Now).TotalMilliseconds)))
					throw new Exception("Attempt to abort failed. Everything's probably broken now.");

			ClearAllWork();

			threads.Clear();
			current = total = updateTasksLock = running = 0;
			finished.Set();

			Start();
		}

		public static bool Cancel(Exception ex = null)
		{
			if (finished.WaitOne(0))
				return false;

			exception = exception ?? ex ?? new OperationCanceledException();
			while (Interlocked.CompareExchange(ref updateTasksLock, 1, 0) == 0) { }

			ClearAllWork();

			updateTasksLock = 0;
			return true;
		}

		static void AddRunning(int count)
		{
			if (Interlocked.Add(ref running, count) == 0)
				finished.Set();
		}

		static void TaskRunnerThread()
		{
			var progress = CreateTaskRunnerProgress();

			ITaskRunnerTask task;
			while (true)
			{
				workReady.WaitOne();
				AddRunning(1);

				try
				{
					while (true)
					{
						task = activeTask;
						if (task == null)
							break;

						if (task.ExecuteDone)
						{
							if (Interlocked.CompareExchange(ref updateTasksLock, 1, 0) != 0)
								continue;

							while (true)
							{
								if (tasks.Count == 0)
								{
									workReady.Reset();
									activeTask = null;
									break;
								}

								task = tasks.Peek();
								if (!task.ExecuteDone)
								{
									activeTask = task;
									break;
								}

								tasks.Pop();
							}

							updateTasksLock = 0;
							continue;
						}

						task.RunBatch(progress);
					}
				}
				catch (Exception ex) when (!(ex is ThreadAbortException)) { Cancel(ex); }

				AddRunning(-1);
			}
		}

		public static void WaitForFinish(Action<double?> setProgress)
		{
			if (finished.WaitOne(0))
				return;

			setProgress?.Invoke(0);
			while (true)
			{
				if (finished.WaitOne(100))
					break;
				setProgress?.Invoke((double)current / total);
			}
			setProgress?.Invoke(null);

			var ex = exception;

			exception = null;
			current = total = 0;

			ThrowIfException(ex);
		}
	}
}
