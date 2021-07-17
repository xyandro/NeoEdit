using System;

namespace NeoEdit.Editor
{
	public class RunOnceAction
	{
		public Action Action { get; private set; }

		public RunOnceAction(Action action) => Action = action;

		public void Invoke()
		{
			if (Action != null)
				lock (this)
					if (Action != null)
					{
						Action();
						Action = null;
					}
		}
	}
}
