using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace NeoEdit.TaskRunning
{
	class TaskRunnerEpic : IDisposable
	{
		readonly ManualResetEvent finishedEvent = new ManualResetEvent(false);
		readonly TaskRunnerTask entryTask;
		readonly Action<double> idleAction;
		public long current, total;
		public Exception exception;

		public TaskRunnerEpic(TaskRunnerTask entryTask, Action<double> idleAction)
		{
			this.entryTask = entryTask;
			this.idleAction = idleAction;
		}

		public void Dispose() => finishedEvent.Dispose();

		internal void ThrowIfException()
		{
			if (exception != null)
				ExceptionDispatchInfo.Capture(exception).Throw();
		}

		public void WaitForFinish()
		{
			while (true)
			{
				if (finishedEvent.WaitOne(100))
					break;

				double progress;
				if (total == 0)
					progress = 0;
				else
					progress = (double)current / total;
				idleAction?.Invoke(progress);
			}
		}

		internal void Cancel(Exception exception) => this.exception = this.exception ?? exception;

		internal void ForceCancel(Exception exception)
		{
			this.exception = exception;
			finishedEvent.Set();
		}

		internal void SetFinished(TaskRunnerTask task)
		{
			if (task == entryTask)
				finishedEvent.Set();
		}
	}
}
