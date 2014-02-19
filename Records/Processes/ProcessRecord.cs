using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Timers;

namespace NeoEdit.Records.Processes
{
	public class ProcessRecord : Record
	{
		static object lockObj = new object();
		static long lastTicks;
		static long curTicks;
		static Dictionary<int, long> lastUsage;
		static Dictionary<int, long> curUsage;
		static ProcessRecord()
		{
			var timer = new Timer(1000);
			timer.Elapsed += (s, e) =>
			{
				lock (lockObj)
				{
					lastTicks = curTicks;
					lastUsage = curUsage;
					curTicks = DateTime.Now.Ticks;
					curUsage = new Dictionary<int, long>();
					foreach (var process in System.Diagnostics.Process.GetProcesses())
					{
						try
						{
							if (process.Id == 0)
								continue;
							curUsage[process.Id] = process.TotalProcessorTime.Ticks;
						}
						catch { }
					}
				}
			};
			timer.Start();
		}

		protected double GetProcessUsage(int PID)
		{
			lock (lockObj)
			{
				if ((lastTicks == 0) || (curTicks == 0))
					return 0;
				if ((!lastUsage.ContainsKey(PID)) || (!curUsage.ContainsKey(PID)))
					return 0;

				return Math.Round((double)(curUsage[PID] - lastUsage[PID]) / (curTicks - lastTicks) * 100, 1);
			}
		}

		public ProcessRecord(string uri) : base(uri) { }

		public override Record Parent
		{
			get
			{
				if (this is ProcessRoot)
					return new Root();
				if (this is Process)
					return new ProcessRoot();
				throw new ArgumentException();
			}
		}

		public override Type GetRootType()
		{
			return typeof(ProcessRecord);
		}

		public static void SuspendProcess(int pid)
		{
			var process = System.Diagnostics.Process.GetProcessById(pid);
			foreach (System.Diagnostics.ProcessThread thread in process.Threads)
			{
				var threadHandle = ProcessInterop.OpenThread(ProcessInterop.ThreadAccess.SUSPEND_RESUME, false, thread.Id);
				if (threadHandle == IntPtr.Zero)
					continue;
				ProcessInterop.SuspendThread(threadHandle);
			}
		}

		public static void ResumeProcess(int pid)
		{
			var process = System.Diagnostics.Process.GetProcessById(pid);
			foreach (System.Diagnostics.ProcessThread thread in process.Threads)
			{
				var threadHandle = ProcessInterop.OpenThread(ProcessInterop.ThreadAccess.SUSPEND_RESUME, false, thread.Id);
				if (threadHandle == IntPtr.Zero)
					continue;
				ProcessInterop.ResumeThread(threadHandle);
			}
		}

		internal static class ProcessInterop
		{
			[Flags]
			public enum ProcessAccessFlags : uint
			{
				PROCESS_ALL_ACCESS = 0x001F0FFF,
				PROCESS_TERMINATE = 0x00000001,
				PROCESS_CREATE_THREAD = 0x00000002,
				PROCESS_VM_OPERATION = 0x00000008,
				PROCESS_VM_READ = 0x00000010,
				PROCESS_VM_WRITE = 0x00000020,
				PROCESS_DUP_HANDLE = 0x00000040,
				PROCESS_SET_INFORMATION = 0x00000200,
				PROCESS_QUERY_INFORMATION = 0x00000400,
				SYNCHRONIZE = 0x00100000
			}

			[Flags]
			public enum ThreadAccess : int
			{
				TERMINATE = 0x0001,
				SUSPEND_RESUME = 0x0002,
				GET_CONTEXT = 0x0008,
				SET_CONTEXT = 0x0010,
				SET_INFORMATION = 0x0020,
				QUERY_INFORMATION = 0x0040,
				SET_THREAD_TOKEN = 0x0080,
				IMPERSONATE = 0x0100,
				DIRECT_IMPERSONATION = 0x0200
			}

			[Flags]
			public enum PageProtect : uint
			{
				PAGE_EXECUTE = 0x10,
				PAGE_EXECUTE_READ = 0x20,
				PAGE_EXECUTE_READWRITE = 0x40,
				PAGE_EXECUTE_WRITECOPY = 0x80,
				PAGE_NOACCESS = 0x01,
				PAGE_READONLY = 0x02,
				PAGE_READWRITE = 0x04,
				PAGE_WRITECOPY = 0x08,
				PAGE_GUARD = 0x100,
				PAGE_NOCACHE = 0x200,
				PAGE_WRITECOMBINE = 0x400
			}

			[Flags]
			public enum MemType : uint
			{
				MEM_IMAGE = 0x1000000,
				MEM_MAPPED = 0x40000,
				MEM_PRIVATE = 0x20000
			}

			[Flags]
			public enum MemoryState : uint
			{
				MEM_COMMIT = 0x1000,
				MEM_FREE = 0x10000,
				MEM_RESERVE = 0x2000
			}

			[StructLayout(LayoutKind.Sequential)]
			public struct MEMORY_BASIC_INFORMATION
			{
				public IntPtr BaseAddress;
				public IntPtr AllocationBase;
				public PageProtect AllocationProtect;
				public IntPtr RegionSize;
				public MemoryState State;
				public PageProtect Protect;
				public MemType Type;
			}

			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, int dwThreadId);
			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern int SuspendThread(IntPtr hThread);
			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern uint ResumeThread(IntPtr hThread);
			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);
			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern bool CloseHandle(IntPtr hObject);
			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern bool VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, int dwLength);
			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, IntPtr dwSize, PageProtect flNewProtect, out PageProtect lpflOldProtect);
			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern bool FlushInstructionCache(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr dwSize);
			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);
			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);
		}
	}
}
