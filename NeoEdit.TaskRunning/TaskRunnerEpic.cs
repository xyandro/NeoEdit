using System.Collections.Generic;
using System.Reflection;

namespace NeoEdit.TaskRunning
{
	class TaskRunnerEpic
	{
		public readonly MethodInfo methodInfo;
		public readonly TaskRunnerEpic parent;
		public readonly int index;

		public readonly List<TaskRunnerTask> tasks = new List<TaskRunnerTask>();
		public readonly List<TaskRunnerEpic> children = new List<TaskRunnerEpic>();
		public int firstNonFinished;
		public long current, total, estimatedTotal, ticks;

		public TaskRunnerEpic(MethodInfo methodInfo = null, TaskRunnerEpic parent = null, int index = 0)
		{
			this.methodInfo = methodInfo;
			this.parent = parent;
			this.index = index;
		}
	}
}
