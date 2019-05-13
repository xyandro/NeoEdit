using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace NeoEdit
{
	public static class ProcessExtension
	{
		const int THREAD_SUSPEND_RESUME = 0x0002;

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern SafeAccessTokenHandle OpenThread(int dwDesiredAccess, bool bInheritHandle, int dwThreadId);
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern int ResumeThread(SafeAccessTokenHandle hThread);
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern int SuspendThread(SafeAccessTokenHandle hThread);

		public static void Suspend(this Process process)
		{
			foreach (ProcessThread thread in process.Threads)
				using (var threadHandle = OpenThread(THREAD_SUSPEND_RESUME, false, thread.Id))
					if (!threadHandle.IsInvalid)
						if (SuspendThread(threadHandle) == -1)
							throw new Win32Exception();
		}

		public static void Resume(this Process process)
		{
			foreach (ProcessThread thread in process.Threads)
				using (var threadHandle = OpenThread(THREAD_SUSPEND_RESUME, false, thread.Id))
					if (!threadHandle.IsInvalid)
						if (ResumeThread(threadHandle) == -1)
							throw new Win32Exception();
		}
	}
}
