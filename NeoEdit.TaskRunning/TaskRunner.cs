using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace NeoEdit.TaskRunning
{
	public static class TaskRunner
	{
		readonly static int NumThreads = Environment.ProcessorCount; // Use 0 to run in calling thread
		const int ForceCancelDelay = 5000;

		public static FluentTaskRunner<T> AsTaskRunner<T>(this IEnumerable<T> items, Action<double> idleAction = null) => new FluentTaskRunner<T>(items, idleAction);
		public static FluentTaskRunner<int> Range(int start, int count, Action<double> idleAction = null) => new FluentTaskRunner<int>(Enumerable.Range(start, count), idleAction);
		public static FluentTaskRunner<T> Repeat<T>(T item, int count, Action<double> idleAction = null) => new FluentTaskRunner<T>(Enumerable.Repeat(item, count), idleAction);

		public static void Run(Action action, Action<double> idleAction = null) => Range(1, 1, idleAction).ForAll((item, index, progress) => action());
		public static void Run(Action<Action<long>> action, Action<double> idleAction = null) => Range(1, 1, idleAction).ForAll((item, index, progress) => action(progress));

		public static T Min<T>(this FluentTaskRunner<T> taskRuner) where T : IComparable => taskRuner.Min(x => x);
		public static T Max<T>(this FluentTaskRunner<T> taskRuner) where T : IComparable => taskRuner.Max(x => x);
		public static FluentTaskRunner<string> NonNullOrWhiteSpace(this FluentTaskRunner<string> taskRuner) => taskRuner.Where(str => !string.IsNullOrWhiteSpace(str));

		readonly static List<Thread> threads = new List<Thread>();
		readonly static Stack<TaskRunnerTask> tasks = new Stack<TaskRunnerTask>();
		readonly static HashSet<TaskRunnerEpic> epics = new HashSet<TaskRunnerEpic>();
		static TaskRunnerTask activeTask;
		static readonly ManualResetEvent workReady = new ManualResetEvent(false);
		[ThreadStatic] static TaskRunnerTask threadActiveTask;
		static int locker;
		static Exception exception;

		static TaskRunner()
		{
			Start();
		}

		static void Start()
		{
			threads.AddRange(Enumerable.Range(0, NumThreads).Select(x => new Thread(RunThread) { Name = $"{nameof(TaskRunner)} {x}" }));
			threads.ForEach(thread => thread.Start());
		}

		internal static void ThrowIfException() => ThrowIfException(exception);
		static void ThrowIfException(Exception ex)
		{
			if (ex != null)
				ExceptionDispatchInfo.Capture(ex).Throw();
		}

		static void RunInCallingThread(TaskRunnerTask task)
		{
			task.epic = new TaskRunnerEpic();
			while (!task.finished)
				task.Run();
		}

		internal static void RunTask(TaskRunnerTask task, Action<double> idleAction)
		{
			ThrowIfException();

			if (NumThreads == 0)
			{
				RunInCallingThread(task);
				return;
			}

			while (Interlocked.CompareExchange(ref locker, 1, 0) != 0) { }

			TaskRunnerEpic epic = null;
			var epicParent = threadActiveTask?.epic;
			if (epicParent != null)
			{
				foreach (var value in epicParent.children)
				{
					if (value.methodInfo == task.methodInfo)
					{
						epic = value;
						break;
					}
				}
			}
			if (epic == null)
			{
				epic = new TaskRunnerEpic(task.methodInfo, epicParent);
				if (epicParent == null)
				{
					epics.Add(epic);
					epic.finishedEvent = new ManualResetEvent(false);
					epic.idleAction = idleAction;
				}
				else
					epicParent.children.Add(epic);
			}

			task.epic = epic;
			Interlocked.Add(ref epic.total, task.totalSize);
			tasks.Push(task);
			activeTask = task;
			workReady.Set();

			locker = 0;

			if (epic.parent == null)
				WaitForFinish(epic);
			else
				RunTasksWhileWaiting(task);
		}

		public static void ForceCancel()
		{
			if (!workReady.WaitOne(0))
				return;

			exception = new Exception($"All active tasks killed. Program may be unstable as a result of this operation.");

			threads.ForEach(thread => thread.Abort());
			var endWait = DateTime.Now.AddMilliseconds(ForceCancelDelay);
			foreach (var thread in threads)
				if (!thread.Join(Math.Max(0, (int)(endWait - DateTime.Now).TotalMilliseconds)))
					throw new Exception("Attempt to abort failed. Everything's probably broken now.");

			threads.Clear();
			tasks.Clear();
			activeTask = null;
			foreach (var epic in epics)
				epic.finishedEvent.Set();
			workReady.Reset();
			locker = 0;

			Start();
		}

		public static bool Cancel(Exception ex = null)
		{
			if (!workReady.WaitOne(0))
				return false;

			if (exception != null)
				return true;

			while (Interlocked.CompareExchange(ref locker, 1, 0) != 0) { }

			if (exception == null)
			{
				exception = ex ?? new OperationCanceledException();
				tasks.Clear();
				activeTask = null;
				foreach (var epic in epics)
					epic.finishedEvent.Set();
				workReady.Reset();
			}

			locker = 0;

			return true;
		}

		static void RunThread()
		{
			while (true)
			{
				workReady.WaitOne();

				threadActiveTask = null;
				try { RunTasksWhileWaiting(); }
				catch (Exception ex) when (!(ex is ThreadAbortException)) { Cancel(ex); }
			}
		}

		static void RunTasksWhileWaiting(TaskRunnerTask waitTask = null)
		{
			var lastActive = threadActiveTask;
			if (lastActive != null)
				Interlocked.Add(ref lastActive.epic.ticks, Timer.Ticks - lastActive.startTicks);

			while ((waitTask == null) || (!waitTask.finished))
			{
				ThrowIfException();

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

			if (lastActive != null)
			{
				lastActive.startTicks = Timer.Ticks;
				threadActiveTask = lastActive;
			}
		}

		static void WaitForFinish(TaskRunnerEpic epic)
		{
			while (true)
			{
				if (epic.finishedEvent.WaitOne(100))
					break;
				if (epic.idleAction != null)
				{
					while (Interlocked.CompareExchange(ref locker, 1, 0) != 0) { }
					var progress = epic.GetProgress();
					locker = 0;

					epic.idleAction(progress);
				}
			}


			epic.finishedEvent.Dispose();

			var ex = exception;

			while (Interlocked.CompareExchange(ref locker, 1, 0) != 0) { }
			epics.Remove(epic);
			if (epics.Count == 0)
				exception = null;
			locker = 0;

			ThrowIfException(ex);
		}
	}
}
