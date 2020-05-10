using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace NeoEdit.TaskRunning
{
	class TaskRunnerEpic
	{
		public readonly MethodInfo methodInfo;
		public readonly TaskRunnerEpic parent;
		public ManualResetEvent finishedEvent;
		public Action<double> idleAction;

		public readonly List<TaskRunnerEpic> children = new List<TaskRunnerEpic>();
		public long current, total, estimatedTotal, ticks;

		public TaskRunnerEpic(MethodInfo methodInfo = null, TaskRunnerEpic parent = null)
		{
			this.methodInfo = methodInfo;
			this.parent = parent;
		}

		internal double GetProgress()
		{
			long sumCurrent = 0, sumTotal = 0;
			GetProgress(ref sumCurrent, ref sumTotal);
			if (sumTotal == 0)
				return 0;
			return (double)sumCurrent / sumTotal;

		}

		internal void GetProgress(ref long sumCurrent, ref long sumTotal)
		{
			if ((parent == null) || (parent.current == 0))
				estimatedTotal = total;
			else
				estimatedTotal = Math.Max(total, total * parent.estimatedTotal / parent.current);
			if (current != 0)
			{
				sumCurrent += ticks;
				sumTotal += ticks * estimatedTotal / current;
			}
			foreach (var child in children)
				child.GetProgress(ref sumCurrent, ref sumTotal);
		}
	}
}
