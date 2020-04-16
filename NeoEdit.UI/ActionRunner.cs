using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NeoEdit.Common;

namespace NeoEdit.UI
{
	public class ActionRunner
	{
		const int DrawFrequency = 5;
		readonly Semaphore semaphore = new Semaphore(0, int.MaxValue);
		readonly Queue<Action<Func<bool>>> actions = new Queue<Action<Func<bool>>>();
		int numSkipped = 0;

		void RunActionsThread()
		{
			while (true)
			{
				try
				{
					semaphore.WaitOne();
					Action<Func<bool>> action;
					lock (semaphore)
						action = actions.Dequeue();
					action(SkipDraw);
				}
				catch { }
			}
		}

		public ActionRunner() => new Thread(RunActionsThread) { Name = nameof(ActionRunner) }.Start();

		public bool SkipDraw()
		{
			lock (semaphore)
			{
				if (actions.Count == 0)
				{
					numSkipped = 0;
					return false;
				}

				++numSkipped;
				if (numSkipped == DrawFrequency)
				{
					numSkipped = 0;
					return false;
				}

				return true;
			}
		}

		public void Add(Action<Func<bool>> action)
		{
			lock (semaphore)
				actions.Enqueue(action);
			semaphore.Release();
		}

		public bool CancelActive()
		{
			lock (semaphore)
			{
				if (!actions.Any())
					return false;

				actions.ForEach(action => semaphore.WaitOne());
				actions.Clear();
				return true;
			}
		}
	}
}
