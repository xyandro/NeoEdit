using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace NeoEdit.Common
{
	public class ProcessBinaryData : IBinaryData
	{
		class Pauser : IDisposable
		{
			ProcessBinaryData parent;
			public Pauser(ProcessBinaryData _parent)
			{
				parent = _parent;
				parent.PauseProcess();
			}

			public void Dispose()
			{
				parent.ResumeProcess();
			}
		}

		Pauser GetPauser()
		{
			return new Pauser(this);
		}

		class Opener : IDisposable
		{
			ProcessBinaryData parent;
			public Opener(ProcessBinaryData _parent)
			{
				parent = _parent;
				parent.OpenProcess();
			}

			public void Dispose()
			{
				parent.CloseProcess();
			}
		}

		Opener GetOpener()
		{
			return new Opener(this);
		}

		public event IBinaryDataChangedDelegate Changed { add { } remove { } }

		readonly int pid;
		public ProcessBinaryData(int PID)
		{
			pid = PID;
		}

		int pauseCount = 0;
		void PauseProcess()
		{
			if (pauseCount++ != 0)
				return;

			var process = Process.GetProcessById(pid);
			foreach (ProcessThread thread in process.Threads)
			{
				var threadHandle = Interop.OpenThread(Interop.ThreadAccess.SUSPEND_RESUME, false, thread.Id);
				if (threadHandle == IntPtr.Zero)
					continue;
				Interop.SuspendThread(threadHandle);
			}
		}

		void ResumeProcess()
		{
			if (--pauseCount != 0)
				return;

			var process = Process.GetProcessById(pid);
			foreach (ProcessThread thread in process.Threads)
			{
				var threadHandle = Interop.OpenThread(Interop.ThreadAccess.SUSPEND_RESUME, false, thread.Id);
				if (threadHandle == IntPtr.Zero)
					continue;
				Interop.ResumeThread(threadHandle);
			}
		}

		IntPtr handle;
		int openCount = 0;
		void OpenProcess()
		{
			if (openCount++ != 0)
				return;

			handle = Interop.OpenProcess(Interop.ProcessAccessFlags.QueryInformation | Interop.ProcessAccessFlags.VMRead | Interop.ProcessAccessFlags.VMOperation, false, pid);
			if (handle == IntPtr.Zero)
				throw new Win32Exception();
		}

		void CloseProcess()
		{
			if (--openCount != 0)
				return;

			if (!Interop.CloseHandle(handle))
				throw new Win32Exception();
		}

		long cacheStart, cacheEnd;
		bool cacheHasData;
		byte[] cache = new byte[65536];
		void SetCache(long address, int minBytes)
		{
			if ((address >= cacheStart) && (address + minBytes <= cacheEnd))
				return;

			if (minBytes > cache.Length)
				throw new ArgumentException("minBytes");

			using (GetPauser())
			using (GetOpener())
			{
				cacheStart = cacheEnd = address;
				cacheHasData = false;

				while (cacheEnd - cacheStart < minBytes)
				{
					bool hasData;
					Interop.MEMORY_BASIC_INFORMATION memInfo;
					if (Interop.VirtualQueryEx(handle, new IntPtr(address), out memInfo, Marshal.SizeOf(typeof(Interop.MEMORY_BASIC_INFORMATION))))
					{
						hasData = (memInfo.State & Interop.MEMORY_BASIC_INFORMATION_STATE.MEM_COMMIT) != 0;
						cacheEnd = memInfo.BaseAddress.ToInt64() + memInfo.RegionSize.ToInt64();
					}
					else
					{
						if (Marshal.GetLastWin32Error() != 87)
							throw new Win32Exception();

						hasData = false;
						cacheEnd = long.MaxValue;
					}

					if ((!hasData) && (!cacheHasData) && (cacheEnd - cacheStart >= minBytes))
						return;

					cacheHasData = true;
					cacheEnd = Math.Min(cacheEnd, cacheStart + cache.Length);

					if (!hasData)
						Array.Clear(cache, (int)(address - cacheStart), (int)(cacheEnd - address));
					else
						using (Interop.RemoveGuard(handle, memInfo))
						{
							var pin = GCHandle.Alloc(cache, GCHandleType.Pinned);
							try
							{
								var ptr = new IntPtr(pin.AddrOfPinnedObject().ToInt64() + address - cacheStart);
								IntPtr read;
								if (!Interop.ReadProcessMemory(handle, new IntPtr(address), ptr, (int)(cacheEnd - address), out read))
									throw new Win32Exception();
							}
							finally
							{
								pin.Free();
							}
						}

					address = cacheEnd;
				}
			}
		}

		public byte this[long index]
		{
			get
			{
				SetCache(index, 1);
				if (!cacheHasData)
					return 0;
				return cache[index - cacheStart];
			}
		}

		public long Length
		{
			get { return long.MaxValue; }
		}

		public bool Find(FindData currentFind, long index, out long start, out long end, bool forward = true)
		{
			start = end = -1;

			Func<byte[], long, byte[], bool, long> findFunc;
			Func<long, long, bool> compareFunc;
			if (forward)
			{
				++index;
				findFunc = Helpers.ForwardArraySearch;
				compareFunc = (a, b) => a < b;
			}
			else
			{
				--index;
				findFunc = Helpers.BackwardArraySearch;
				compareFunc = (a, b) => a > b;
			}
			if ((index < 0) || (index >= Length))
				return false;

			var findLen = currentFind.Data.Select(bytes => bytes.Length).Max();

			using (GetPauser())
			using (GetOpener())
			{
				while (index < Length)
				{
					SetCache(index, findLen);
					if (cacheHasData)
					{
						for (var findPos = 0; findPos < currentFind.Data.Count; findPos++)
						{
							var found = findFunc(cache, index - cacheStart, currentFind.Data[findPos], currentFind.IgnoreCase[findPos]);
							if ((found != -1) && ((start == -1) || (compareFunc(found, start))))
							{
								start = found + cacheStart;
								end = start + currentFind.Data[findPos].Length;
							}
						}

						if (start != -1)
							return true;
					}

					index = cacheEnd;
					if (index != long.MaxValue)
						index -= findLen - 1;
				}

				return false;
			}
		}

		public void Replace(long index, long count, byte[] bytes)
		{
			throw new NotImplementedException();
		}

		public byte[] GetAllBytes()
		{
			throw new NotImplementedException();
		}

		public byte[] GetSubset(long index, long count)
		{
			using (GetPauser())
			{
				var result = new byte[count];
				while (count > 0)
				{
					SetCache(index, 1);
					var len = Math.Min(count, cacheEnd - index);

					if (cache != null)
						Array.Copy(cache, index - cacheStart, result, result.Length - count, len);

					index += len;
					count -= len;
				}

				return result;
			}
		}
	}

	static class Interop
	{
		[Flags]
		public enum ProcessAccessFlags : uint
		{
			All = 0x001F0FFF,
			Terminate = 0x00000001,
			CreateThread = 0x00000002,
			VMOperation = 0x00000008,
			VMRead = 0x00000010,
			VMWrite = 0x00000020,
			DupHandle = 0x00000040,
			SetInformation = 0x00000200,
			QueryInformation = 0x00000400,
			Synchronize = 0x00100000
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
		public enum PageProtectEnum : uint
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
		public enum MEMORY_BASIC_INFORMATION_STATE : uint
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
			public PageProtectEnum AllocationProtect;
			public IntPtr RegionSize;
			public MEMORY_BASIC_INFORMATION_STATE State;
			public PageProtectEnum Protect;
			public uint Type;
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
		//[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CloseHandle(IntPtr hObject);
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, int dwLength);
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, IntPtr dwSize, PageProtectEnum flNewProtect, out PageProtectEnum lpflOldProtect);
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

		public class OnCloseAction : IDisposable
		{
			Action closeAction;
			public OnCloseAction(Action _closeAction)
			{
				closeAction = _closeAction;
			}

			public void Dispose()
			{
				closeAction();
			}
		}

		public static OnCloseAction RemoveGuard(IntPtr processHandle, MEMORY_BASIC_INFORMATION memInfo)
		{
			Action action = () => { };

			if ((memInfo.Protect & PageProtectEnum.PAGE_GUARD) != 0)
			{
				PageProtectEnum oldProtect;
				if (!VirtualProtectEx(processHandle, memInfo.BaseAddress, memInfo.RegionSize, PageProtectEnum.PAGE_READWRITE, out oldProtect))
					throw new Win32Exception();
				action = () => Interop.VirtualProtectEx(processHandle, memInfo.BaseAddress, memInfo.RegionSize, memInfo.Protect, out oldProtect);
			}

			return new OnCloseAction(action);
		}
	}
}
