using System.Threading;

namespace NeoEdit.Editor
{
	public class ShutdownData
	{
		string name;
		int count;

		public ShutdownData(string name, int count)
		{
			this.name = name;
			this.count = count;
			CheckRelease(count);
		}

		public void OnShutdown() => CheckRelease(Interlocked.Decrement(ref count));

		void CheckRelease(int count)
		{
			if (count == 0)
				new EventWaitHandle(false, EventResetMode.ManualReset, name).Set();
		}
	}
}
