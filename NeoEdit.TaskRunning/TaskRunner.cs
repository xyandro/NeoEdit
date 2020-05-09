using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace NeoEdit.TaskRunning
{
	public static class TaskRunner
	{
		readonly static int NumThreads = Environment.ProcessorCount; // Use 0 to run in calling thread
		const int ForceCancelDelay = 5000;

		public static FluentTaskRunner<T> AsTaskRunner<T>(this IEnumerable<T> items) => new FluentTaskRunner<T>(items);
		public static FluentTaskRunner<int> Range(int start, int count) => new FluentTaskRunner<int>(Enumerable.Range(start, count));
		public static FluentTaskRunner<T> Repeat<T>(T item, int count) => new FluentTaskRunner<T>(Enumerable.Repeat(item, count));

		public static void Run(Action action) => Range(1, 1).ForAll((item, index, progress) => action());
		public static void Run(Action<Action<long>> action) => Range(1, 1).ForAll((item, index, progress) => action(progress));

		public static T Min<T>(this FluentTaskRunner<T> taskRuner) where T : IComparable => taskRuner.Min(x => x);
		public static T Max<T>(this FluentTaskRunner<T> taskRuner) where T : IComparable => taskRuner.Max(x => x);
		public static FluentTaskRunner<string> NonNullOrWhiteSpace(this FluentTaskRunner<string> taskRuner) => taskRuner.Where(str => !string.IsNullOrWhiteSpace(str));

		readonly static List<Thread> threads = new List<Thread>();
		readonly static List<TaskRunnerEpic> epics = new List<TaskRunnerEpic>();
		static TaskRunnerTask activeTask;
		static readonly ManualResetEvent workReady = new ManualResetEvent(false);
		static readonly ManualResetEvent finished = new ManualResetEvent(true);
		[ThreadStatic] static TaskRunnerTask threadActiveTask;
		static int locker, running;
		static bool isFinished;
		static Exception exception;

		public static Action<double> IdleAction { get; set; }

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
			task.epic.tasks.Add(task);
			while (!task.finished)
				task.Run();
		}

		internal static void RunTask(TaskRunnerTask task)
		{
			if (NumThreads == 0)
			{
				RunInCallingThread(task);
				return;
			}

			while (Interlocked.CompareExchange(ref locker, 1, 0) != 0) { }

			var inTaskRunner = threadActiveTask != null;

			TaskRunnerEpic epicParent;
			if (inTaskRunner)
				epicParent = threadActiveTask.epic;
			else
			{
				if (epics.Count != 0)
					throw new Exception("Can't start multiple taskrunners");

				epicParent = new TaskRunnerEpic();
				finished.Reset();
			}

			TaskRunnerEpic epic = default;
			foreach (var value in epicParent.children)
			{
				if (value.methodInfo == task.methodInfo)
				{
					epic = value;
					break;
				}
			}
			if (epic == null)
			{
				epic = new TaskRunnerEpic(task.methodInfo, epicParent, epics.Count);
				epics.Add(epic);
				epicParent.children.Add(epic);
			}
			task.epic = epic;
			task.index = epic.tasks.Count;
			Interlocked.Add(ref epic.total, task.totalSize);
			epic.tasks.Add(task);
			SetActiveTask(task, false);
			workReady.Set();

			locker = 0;

			if (inTaskRunner)
				RunTasksWhileWaiting(task);
			else
				WaitForFinish();
		}

		internal static void SetActiveTask(TaskRunnerTask task, bool getLock)
		{
			if (getLock)
				while (Interlocked.CompareExchange(ref locker, 1, 0) != 0) { }

			if (exception != null)
			{
				locker = 0;
				ThrowIfException();
			}

			if ((activeTask == null) || (task.epic.index > activeTask.epic.index) || ((task.epic.index == activeTask.epic.index) && (task.index < activeTask.index)))
				activeTask = task;

			if (getLock)
				locker = 0;
		}

		static void MoveToNextTask(TaskRunnerTask task)
		{
			if (Interlocked.CompareExchange(ref locker, 1, 0) != 0)
				return;

			if (exception != null)
			{
				locker = 0;
				ThrowIfException();
			}

			if (activeTask != task)
			{
				locker = 0;
				return;
			}

			var epic = task.epic;
			var epicIndex = task.epic.index;
			var taskIndex = task.index;
			task = epic.tasks[taskIndex];
			while (true)
			{
				if (task == null)
				{
					if (taskIndex == epic.firstNonFinished)
						epic.tasks[epic.firstNonFinished++] = null;
				}
				else if (task.canRun)
				{
					activeTask = task;
					break;
				}

				++taskIndex;
				while (taskIndex == epic.tasks.Count)
				{
					if (epicIndex == 0)
					{
						activeTask = null;
						if (epics[0].tasks[0] == null)
							isFinished = true;
						locker = 0;
						return;
					}
					--epicIndex;
					epic = epics[epicIndex];
					taskIndex = epic.firstNonFinished;
				}
				task = epic.tasks[taskIndex];
			}

			locker = 0;
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

			threads.Clear();
			activeTask = null;
			workReady.Reset();
			locker = running = 0;
			finished.Set();

			Start();
		}

		static void ReleaseLock([CallerMemberName] string caller = null)
		{
			Console.WriteLine($"{caller} releasing lock");
			locker = 0;
		}

		public static bool Cancel(Exception ex = null)
		{
			if (finished.WaitOne(0))
				return false;

			if (exception != null)
				return true;

			while (Interlocked.CompareExchange(ref locker, 1, 0) != 0) { }

			if (exception == null)
			{
				exception = ex ?? new OperationCanceledException();
				isFinished = true;
				activeTask = null;
			}

			locker = 0;

			return true;
		}

		static void RunThread()
		{
			while (true)
			{
				finished.WaitOne();
				workReady.WaitOne();

				Interlocked.Increment(ref running);
				try { RunTasksWhileWaiting(); }
				catch (Exception ex) when (!(ex is ThreadAbortException)) { Cancel(ex); }
				if (Interlocked.Decrement(ref running) == 0)
				{
					workReady.Reset();
					finished.Set();
				}
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
					if (isFinished)
						break;
					continue;
				}

				if (!task.canRun)
				{
					MoveToNextTask(task);
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

		static void WaitForFinish()
		{
			while (true)
			{
				if (finished.WaitOne(100))
					break;
				RunIdleAction();
			}

			var ex = exception;
			exception = null;
			isFinished = false;
			epics.Clear();
			IdleAction = null;
			ThrowIfException(ex);
		}

		internal static void RunIdleAction()
		{
			if (IdleAction == null)
				return;

			while (Interlocked.CompareExchange(ref locker, 1, 0) != 0) { }
			long current = 0, total = 0;
			foreach (var epic in epics)
			{
				if ((epic.parent == null) || (epic.parent.current == 0))
					epic.estimatedTotal = epic.total;
				else
					epic.estimatedTotal = Math.Max(epic.total, epic.total * epic.parent.estimatedTotal / epic.parent.current);
				if (epic.current != 0)
				{
					current += epic.ticks;
					total += epic.ticks * epic.estimatedTotal / epic.current;
				}
			}
			locker = 0;

			double value;
			if (total == 0)
				value = 0;
			else
				value = (double)current / total;
			try { IdleAction(value); } catch { }
		}
	}
}
