using System;
using System.Collections;

namespace NeoEdit.TaskRunning
{
	abstract class TaskRunnerTask
	{
		internal TaskRunnerEpic epic;
		internal bool canRun = true;
		internal bool finished;
		internal long totalSize;

		internal abstract void RunTask(IEnumerable itemsEnum, Action<double> idleAction = null);
		internal abstract void Run();
	}
}
