using System;
using System.Collections;

namespace NeoEdit.TaskRunning
{
	abstract class TaskRunnerTask
	{
		public TaskRunnerEpic epic;
		public bool canRun = true;
		public bool finished;
		public long totalSize;

		public abstract void RunTask(IEnumerable itemsEnum, Action<double> idleAction = null);
		public abstract void Run();
	}
}
