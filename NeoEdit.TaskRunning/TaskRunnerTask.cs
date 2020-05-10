using System;
using System.Collections;
using System.Reflection;

namespace NeoEdit.TaskRunning
{
	abstract class TaskRunnerTask
	{
		public readonly MethodInfo methodInfo;
		public TaskRunnerEpic epic;
		public bool canRun = true;
		public bool finished;
		public long startTicks;
		public long totalSize;

		public TaskRunnerTask(MethodInfo methodInfo) => this.methodInfo = methodInfo;

		public abstract void RunTask(IEnumerable itemsEnum, Action<double> idleAction = null);
		public abstract void Run();
	}
}
