using System;

namespace NeoEdit.Common
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
					threadRandom = new Random(mainRandom.Next());
			return threadRandom.Next();
		}

		public int Next(int maxValue)
		{
			if (threadRandom == null)
				lock (mainRandom)
					threadRandom = new Random(mainRandom.Next());
			return threadRandom.Next(maxValue);
		}

		public int Next(int minValue, int maxValue)
		{
			if (threadRandom == null)
				lock (mainRandom)
					threadRandom = new Random(mainRandom.Next());
			return threadRandom.Next(minValue, maxValue);
		}
	}
}
