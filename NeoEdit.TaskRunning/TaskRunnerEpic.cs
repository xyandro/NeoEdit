using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace NeoEdit.TaskRunning
{
	class TaskRunnerEpic
	{
		readonly ManualResetEvent finishedEvent = new ManualResetEvent(false);
		readonly TaskRunnerTask entryTask;
		readonly Action<double> idleAction;
		internal long current, total;
		internal Exception exception;

		internal TaskRunnerEpic(TaskRunnerTask entryTask, Action<double> idleAction)
		{
			this.entryTask = entryTask;
			this.idleAction = idleAction;
		}

		internal void ThrowIfException()
		{
			if (exception != null)
				ExceptionDispatchInfo.Capture(exception).Throw();
		}

		internal void WaitForFinish()
		{
			while (true)
			{
				if (finishedEvent.WaitOne(100))
				{
					finishedEvent.Dispose();
					return;
				}

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
