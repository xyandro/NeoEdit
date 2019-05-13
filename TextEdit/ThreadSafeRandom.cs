using System;

namespace NeoEdit.TextEdit
{
	public class ThreadSafeRandom
	{
		static Random mainRandom = new Random();
		[ThreadStatic]
		static Random threadRandom;

		public int Next()
		{
			if (threadRandom == null)
				lock (mainRandom)
					if (threadRandom == null)
						threadRandom = new Random(mainRandom.Next());
			return threadRandom.Next();
		}

		public int Next(int maxValue)
		{
			if (threadRandom == null)
				lock (mainRandom)
					if (threadRandom == null)
						threadRandom = new Random(mainRandom.Next());
			return threadRandom.Next(maxValue);
		}

		public int Next(int minValue, int maxValue)
		{
			if (threadRandom == null)
				lock (mainRandom)
					if (threadRandom == null)
						threadRandom = new Random(mainRandom.Next());
			return threadRandom.Next(minValue, maxValue);
		}
	}
}
