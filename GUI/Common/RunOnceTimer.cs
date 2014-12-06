using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;

namespace NeoEdit.GUI.Common
{
	// As this is always run on the dispatcher thread, multi-threaded locking isn't necessary.
	public class RunOnceTimer
	{
		readonly Action action;
		readonly HashSet<RunOnceTimer> dependencies = new HashSet<RunOnceTimer>();
		DispatcherTimer timer = null;
		public RunOnceTimer(Action action)
		{
			this.action = action;
		}

		public void AddDependency(params RunOnceTimer[] timers)
		{
			foreach (var timer in timers)
				if (timer == this)
					throw new ArgumentException("Cannot add self as dependency.");
				else
					dependencies.Add(timer);
		}

		public bool Started { get { return timer != null; } }

		public void Start()
		{
			if (Started)
				return;

			timer = new DispatcherTimer();
			timer.Tick += (s, e) =>
			{
				Stop();

				if (dependencies.Any(dependency => dependency.Started))
				{
					// A dependency is ready to go; queue up for after it's done
					Start();
					return;
				}

				action();
			};
			timer.Start();
		}

		public void Stop()
		{
			if (!Started)
				return;

			timer.Stop();
			timer = null;
		}
	}
}
