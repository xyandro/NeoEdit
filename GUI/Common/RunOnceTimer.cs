using System;
using System.Windows.Threading;

namespace NeoEdit.GUI.Common
{
	// As this is always run on the dispatcher thread, multi-threaded locking isn't necessary.
	public class RunOnceTimer
	{
		readonly Action action;
		DispatcherTimer timer = null;
		public RunOnceTimer(Action action)
		{
			this.action = action;
		}

		public bool Started()
		{
			return timer != null;
		}

		public void Start()
		{
			if (Started())
				return;

			timer = new DispatcherTimer();
			timer.Tick += (s, e) =>
			{
				timer.Stop();
				timer = null;

				action();
			};
			timer.Start();
		}
	}
}
