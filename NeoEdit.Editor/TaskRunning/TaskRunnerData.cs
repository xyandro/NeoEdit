using System;
using System.Collections.Generic;
using NeoEdit.Common;

namespace NeoEdit.Editor.TaskRunning
{
	class TaskRunnerData
	{
		public IEnumerator<object> Enumerator { get; set; }
		public int Count { get; set; }
		public int NextIndex { get; set; }
		public Func<object, ITaskRunnerProgress, object> Func { get; set; }
		public Action<int, object, ITaskRunnerProgress> Action { get; set; }
	}
}
