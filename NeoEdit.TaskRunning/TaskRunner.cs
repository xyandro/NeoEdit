using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace NeoEdit.TaskRunning
{
	public static class TaskRunner
	{
		static readonly int NumThreads = Environment.ProcessorCount; // Use 0 to run in calling thread
		const int ForceCancelDelay = 5000;

		public static FluentTaskRunner<T> AsTaskRunner<T>(this IEnumerable<T> items, Action<double> idleAction = null) => new FluentTaskRunner<T>(items, idleAction);
		public static FluentTaskRunner<int> Range(int start, int count, Action<double> idleAction = null) => new FluentTaskRunner<int>(Enumerable.Range(start, count), idleAction);
		public static FluentTaskRunner<T> Repeat<T>(T item, int count, Action<double> idleAction = null) => new FluentTaskRunner<T>(Enumerable.Repeat(item, count), idleAction);

		public static void Run(Action action, Action<double> idleAction = null) => Range(1, 1, idleAction).ForAll((item, index, progress) => action());
		public static void Run(Action<Action<long>> action, Action<double> idleAction = null) => Range(1, 1, idleAction).ForAll((item, index, progress) => action(progress));

		public static T Min<T>(this FluentTaskRunner<T> taskRuner) where T : IComparable => taskRuner.Min(x => x);
		public static T Max<T>(this FluentTaskRunner<T> taskRuner) where T : IComparable => taskRuner.Max(x => x);
		public static FluentTaskRunner<string> NonNullOrWhiteSpace(this FluentTaskRunner<string> taskRuner) => taskRuner.Where(str => !string.IsNullOrWhiteSpace(str));

		static readonly List<Thread> threads = new List<Thread>();
		static readonly Stack<TaskRunnerTask> tasks = new Stack<TaskRunnerTask>();
		static readonly HashSet<TaskRunnerEpic> epics = new HashSet<TaskRunnerEpic>();
		static TaskRunnerTask activeTask;
		static readonly ManualResetEvent workReady = new ManualResetEvent(false);
		[ThreadStatic] static TaskRunnerTask threadActiveTask;
		static int locker;

		static TaskRunner()
		{
			Start();
		}

		static void Start()
		{
			threads.AddRange(Enumerable.Range(0, NumThreads).Select(x => new Thread(RunThread) { Name = $"{nameof(TaskRunner)} {x}" }));
			threads.ForEach(thread => thread.Start());
		}

		static void RunInCallingThread(TaskRunnerTask task)
		{
			task.epic = new TaskRunnerEpic(task, null);
			while (!task.finished)
				task.Run();
		}

		internal static void RunTask(TaskRunnerTask task, Action<double> idleAction)
		{
			if (NumThreads == 0)
			{
				RunInCallingThread(task);
				return;
			}

			var epic = threadActiveTask?.epic;
			var createdEpic = false;
			if (epic == null)
			{
				epic = new TaskRunnerEpic(task, idleAction);
				createdEpic = true;
			}

			task.epic = epic;
			Interlocked.Add(ref epic.total, task.totalSize);

			while (Interlocked.CompareExchange(ref locker, 1, 0) != 0) { }

			if (createdEpic)
				epics.Add(epic);

			tasks.Push(task);
			activeTask = task;
			workReady.Set();

			locker = 0;

			if (createdEpic)
				WaitForEpic(epic);
			else
				RunTasksWhileWaiting(task);
		}

		public static void ForceCancel()
		{
			if (epics.Count == 0)
				return;

			threads.ForEach(thread => thread.Abort());
			var endWait = DateTime.Now.AddMilliseconds(ForceCancelDelay);
			foreach (var thread in threads)
				if (!thread.Join(Math.Max(0, (int)(endWait - DateTime.Now).TotalMilliseconds)))
					throw new Exception("Attempt to abort failed. Everything's probably broken now. (Pressing Ctrl+Break again may fix the problem.)");

			threads.Clear();
			tasks.Clear();
			activeTask = null;
			var exception = new Exception($"All active tasks killed. Program may be unstable as a result of this operation.");
			foreach (var epic in epics)
				epic.ForceCancel(exception);
			workReady.Reset();
			locker = 0;

			Start();
		}

		public static bool Cancel(Exception ex = null)
		{
			if (epics.Count == 0)
				return false;

			while (Interlocked.CompareExchange(ref locker, 1, 0) != 0) { }

			var exception = ex ?? new OperationCanceledException();
			foreach (var epic in epics)
				epic.Cancel(exception);

			locker = 0;

			return true;
		}

		static void RunThread()
		{
			while (true)
			{
				workReady.WaitOne();
				RunTasksWhileWaiting();
			}
		}

		static void RunTasksWhileWaiting(TaskRunnerTask waitTask = null)
		{
			var lastActive = threadActiveTask;

			while ((waitTask == null) || (!waitTask.finished))
			{
				var task = activeTask;
				if (task == null)
				{
					if (waitTask == null)
						break;
					continue;
				}

				if (!task.canRun)
				{
					if (Interlocked.CompareExchange(ref locker, 1, 0) != 0)
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
						if (!task.canRun)
						{
							tasks.Pop();
							continue;
						}

						activeTask = task;
						break;
					}

					locker = 0;
					continue;
				}

				threadActiveTask = task;
				task.Run();
			}

			threadActiveTask = lastActive;

			waitTask?.epic.ThrowIfException();
		}

		static void WaitForEpic(TaskRunnerEpic epic)
		{
			epic.WaitForFinish();

			while (Interlocked.CompareExchange(ref locker, 1, 0) != 0) { }
			epics.Remove(epic);
			locker = 0;

			epic.ThrowIfException();
		}
	}
}
