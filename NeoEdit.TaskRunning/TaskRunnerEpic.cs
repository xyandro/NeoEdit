using System;
using System.Threading;

namespace NeoEdit.TaskRunning
{
	class TaskRunnerEpic : IDisposable
	{
		public readonly ManualResetEvent finishedEvent = new ManualResetEvent(false);
		public readonly TaskRunnerTask entryTask;
		public long current, total;
		readonly Action<double> idleAction;

		public TaskRunnerEpic(TaskRunnerTask entryTask, Action<double> idleAction)
		{
			this.entryTask = entryTask;
			this.idleAction = idleAction;
		}

		public void Dispose() => finishedEvent.Dispose();

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
	}
}
