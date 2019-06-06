using System.Threading;

namespace NeoEdit
{
	public class ShutdownData
	{
		string name;
		int count;

		public ShutdownData(string name, int count)
		{
			this.name = name;
			this.count = count;
		}

		public void OnShutdown()
		{
			lock (this)
				if (--count == 0)
					new EventWaitHandle(false, EventResetMode.ManualReset, name).Set();
		}
	}
}
