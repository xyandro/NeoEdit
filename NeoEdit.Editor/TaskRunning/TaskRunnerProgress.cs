using System;
using NeoEdit.Common;

namespace NeoEdit.Editor.TaskRunning
{
	class TaskRunnerProgress : ITaskRunnerProgress
	{
		public Action<long, long> SetProgressAction;

		public void SetProgress(long current, long total) => SetProgressAction(current, total);
	}
}
