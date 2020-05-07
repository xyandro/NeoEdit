using System.Runtime.InteropServices;

namespace NeoEdit.TaskRunning
{
	static class Timer
	{
		public static long Ticks
		{
			get
			{
				Win32.QueryPerformanceCounter(out var value);
				return value;
			}
		}

		static class Win32
		{
			[DllImport("kernel32.dll", SetLastError = true)]
			static public extern bool QueryPerformanceCounter(out long lpPerformanceCount);
		}
	}
}
