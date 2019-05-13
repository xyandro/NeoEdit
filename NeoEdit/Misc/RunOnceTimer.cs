using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;

namespace NeoEdit.Misc
{
	public class RunOnceTimer
	{
		static DispatcherTimer timer = null;
		static readonly HashSet<RunOnceTimer> ready = new HashSet<RunOnceTimer>();

		readonly Action action;
		readonly HashSet<RunOnceTimer> dependencies = new HashSet<RunOnceTimer>();
		public RunOnceTimer(Action action) { this.action = action; }

		public void AddDependency(params RunOnceTimer[] timers)
		{
			foreach (var timer in timers)
				if (timer == this)
					throw new ArgumentException("Cannot add self as dependency.");
				else
					dependencies.Add(timer);
		}

		public bool Started { get; private set; }

		public void Start()
		{
			if (Started)
				return;

			Started = true;
			ready.Add(this);
			SetupTimer();
		}

		public void Stop()
		{
			if (!Started)
				return;

			Started = false;
			ready.Remove(this);
			SetupTimer();
		}

		static void SetupTimer()
		{
			if ((timer == null) != (ready.Any()))
				return;

			if (timer == null)
			{
				timer = new DispatcherTimer();
				timer.Tick += OnTimer;
				timer.Start();
			}
			else
			{
				timer.Stop();
				timer = null;
			}
		}

		static void OnTimer(object sender, EventArgs e)
		{
			while (true)
			{
				if (!ready.Any())
					break;
				foreach (var timer in ready.ToList())
				{
					if (timer.dependencies.Any(dependency => dependency.Started))
						continue;

					timer.Stop();
					timer.action();
				}
			}
		}
	}
}
