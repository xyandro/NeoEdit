using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Editor.TaskRunning
{
	class Transform
	{
		static readonly List<object> EmptyList = new List<object>();
		class TransformTask
		{
			public object Value { get; set; }
			public IReadOnlyList<object> Results { get; set; }

			public override string ToString() => Value?.ToString() ?? "";
		}

		public Action<IEnumerable<object>> Runner { get; set; }

		public Func<object, ITaskRunnerProgress, IEnumerable<object>> DoTask { get; set; }

		public Transform()
		{
			Runner = DefaultRunner;
			DoTask = (obj, progress) => EmptyList;
		}

		public void Run()
		{
			lock (this)
				Runner(Tasks.Select(task => task.Value));
		}

		Queue<TransformTask> Tasks { get; } = new Queue<TransformTask>();
		public Transform NextTransform { get; set; }

		void DefaultRunner(IEnumerable<object> _)
		{
			foreach (var task in Tasks)
			{
				TaskRunner.AddTask(progress =>
				{
					var results = DoTask(task.Value, progress).ToList();

					lock (this)
					{
						task.Results = results;

						while (Tasks.Count > 0)
						{
							var finished = Tasks.Peek();
							if (finished.Results == null)
								break;
							Tasks.Dequeue();

							NextTransform?.AddTasks(finished.Results);
						}

						if (Tasks.Count == 0)
							NextTransform?.Run();
					}
				});
			}
			if (!Tasks.Any())
				TaskRunner.AddTask(progress => NextTransform?.Run());
		}

		public void AddTasks(IEnumerable<object> results)
		{
			lock (this)
				results.ForEach(result => Tasks.Enqueue(new TransformTask { Value = result }));
		}
	}
}
