using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	static public class TaskRunner
	{
		const int DelayBeforeUI = 500;
		const int NumThreads = 9;

		static readonly IReadOnlyList<TaskProgress> progresses = new List<TaskProgress>(Enumerable.Range(0, NumThreads + 1).Select(x => new TaskProgress()));

		class Task
		{
			public Func<TaskProgress, object> FuncToRun { get; set; }
			public Action<object> NextAction { get; set; }
			public object Result { get; set; }
			public bool Done { get; set; }
		}
		static readonly List<Task> tasks = new List<Task>();
		static int nextTask = 0;
		static Semaphore taskSemaphore = new Semaphore(0, int.MaxValue);

		static int nextResult = 0;
		static Semaphore resultSemaphore = new Semaphore(0, int.MaxValue);

		static Exception exception;

		static ManualResetEvent finished = new ManualResetEvent(true);

		static TaskRunner()
		{
			progresses[0].Name = "Overall";
			progresses[0].Working = true;

			new Thread(ResultsThread).Start();
			Enumerable.Range(0, NumThreads).ForEach(x => new Thread(() => WorkerThread(progresses[x + 1])).Start());
		}

		static void Add(Task task)
		{
			finished.Reset();
			lock (tasks)
				tasks.Add(task);
			taskSemaphore.Release();
		}

		static void Add(IEnumerable<Task> newTasks)
		{
			finished.Reset();
			var useTasks = newTasks.ToList();
			lock (tasks)
				tasks.AddRange(useTasks);
			taskSemaphore.Release(useTasks.Count);
		}

		static public void Add(Action action) => Add(new Task { FuncToRun = progress => { action(); return null; } });
		static public void Add(Action action, Action next) => Add(new Task { FuncToRun = progress => { action(); return null; }, NextAction = obj => next() });
		static public void Add(Action<TaskProgress> action) => Add(new Task { FuncToRun = progress => { action(progress); return null; } });
		static public void Add(Action<TaskProgress> action, Action next) => Add(new Task { FuncToRun = progress => { action(progress); return null; }, NextAction = obj => next() });
		static public void Add(Func<object> func) => Add(new Task { FuncToRun = progress => func() });
		static public void Add(Func<object> func, Action<object> next) => Add(new Task { FuncToRun = progress => func(), NextAction = next });
		static public void Add(Func<TaskProgress, object> func) => Add(new Task { FuncToRun = func });
		static public void Add(Func<TaskProgress, object> func, Action<object> next) => Add(new Task { FuncToRun = func, NextAction = next });
		static public void Add<T>(Func<T> func) => Add(new Task { FuncToRun = progress => func() });
		static public void Add<T>(Func<T> func, Action<T> next) => Add(new Task { FuncToRun = progress => func(), NextAction = result => next((T)result) });
		static public void Add<T>(Func<TaskProgress, T> func) => Add(new Task { FuncToRun = progress => func(progress) });
		static public void Add<T>(Func<TaskProgress, T> func, Action<T> next) => Add(new Task { FuncToRun = progress => func(progress), NextAction = result => next((T)result) });

		static public void Add(IEnumerable<Action> actions) => Add(actions.Select(action => new Task { FuncToRun = progress => { action(); return null; } }));
		static public void Add(IEnumerable<Action> actions, Action next) => Add(actions.Select(action => new Task { FuncToRun = progress => { action(); return null; }, NextAction = obj => next() }));
		static public void Add(IEnumerable<Action<TaskProgress>> actions) => Add(actions.Select(action => new Task { FuncToRun = progress => { action(progress); return null; } }));
		static public void Add(IEnumerable<Action<TaskProgress>> actions, Action next) => Add(actions.Select(action => new Task { FuncToRun = progress => { action(progress); return null; }, NextAction = obj => next() }));
		static public void Add(IEnumerable<Func<object>> funcs) => Add(funcs.Select(func => new Task { FuncToRun = progress => func() }));
		static public void Add(IEnumerable<Func<object>> funcs, Action<object> next) => Add(funcs.Select(func => new Task { FuncToRun = progress => func(), NextAction = next }));
		static public void Add(IEnumerable<Func<TaskProgress, object>> funcs) => Add(funcs.Select(func => new Task { FuncToRun = func }));
		static public void Add(IEnumerable<Func<TaskProgress, object>> funcs, Action<object> next) => Add(funcs.Select(func => new Task { FuncToRun = func, NextAction = next }));
		static public void Add<T>(IEnumerable<Func<T>> funcs) => Add(funcs.Select(func => new Task { FuncToRun = progress => func() }));
		static public void Add<T>(IEnumerable<Func<T>> funcs, Action<T> next) => Add(funcs.Select(func => new Task { FuncToRun = progress => func(), NextAction = result => next((T)result) }));
		static public void Add<T>(IEnumerable<Func<TaskProgress, T>> funcs) => Add(funcs.Select(func => new Task { FuncToRun = progress => func(progress) }));
		static public void Add<T>(IEnumerable<Func<TaskProgress, T>> funcs, Action<T> next) => Add(funcs.Select(func => new Task { FuncToRun = progress => func(progress), NextAction = result => next((T)result) }));

		static public void AddResult(Action next) => Add(new Task { NextAction = obj => next() });

		static void Cancel(Exception ex)
		{
			exception = ex;
			progresses.ForEach(progress => progress.Cancel = true);
		}

		static void WorkerThread(TaskProgress progress)
		{
			while (true)
			{
				taskSemaphore.WaitOne();
				Task task;
				lock (tasks)
					task = tasks[nextTask++];
				if ((task.FuncToRun != null) && (!progress.Cancel))
				{
					progress.Working = true;
					try { task.Result = task.FuncToRun(progress); }
					catch (Exception ex) { Cancel(ex); }
					progress.Working = false;
					progress.Name = null;
					progress.Percent = null;
				}
				task.Done = true;
				resultSemaphore.Release();
			}
		}

		static void ResultsThread()
		{
			var useCount = 0;
			var useTasks = new List<Task>();
			var isLast = false;
			while (true)
			{
				progresses[0].Percent = (double)nextResult / tasks.Count;

				resultSemaphore.WaitOne();
				++useCount;
				lock (tasks)
				{
					while ((useCount > 0) && (tasks[nextResult].Done))
					{
						useTasks.Add(tasks[nextResult++]);
						--useCount;
					}
					isLast = nextResult == tasks.Count;
				}
				useTasks.ForEach(task => task.NextAction?.Invoke(task.Result));
				useTasks.Clear();

				if (isLast)
				{
					lock (tasks)
					{
						if (nextResult == tasks.Count)
							finished.Set();
					}
				}
			}
		}

		static public void WaitForFinish(ITabsWindow tabsWindow)
		{
			// Lock so only one instance can be waiting
			lock (finished)
			{
				if (!finished.WaitOne(DelayBeforeUI))
					if (!tabsWindow.RunTaskRunnerDialog(progresses, finished))
					{
						Cancel(new OperationCanceledException());
						finished.WaitOne();
					}

				lock (tasks)
				{
					var toThrow = exception;
					exception = null;
					tasks.Clear();
					nextTask = nextResult = 0;
					progresses.ForEach(progress => progress.Cancel = false);
					if (toThrow != null)
						throw toThrow;
				}
			}
		}
	}
}
