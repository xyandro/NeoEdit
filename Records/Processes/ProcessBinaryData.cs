﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using NeoEdit.Common;

namespace NeoEdit.Records.Processes
{
	public class ProcessBinaryData : IBinaryData
	{
		class OnCloseAction : IDisposable
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

		OnCloseAction Suspend()
		{
			SuspendProcess();
			return new OnCloseAction(() => ResumeProcess());
		}

		OnCloseAction Open()
		{
			OpenProcess();
			return new OnCloseAction(() => CloseProcess());
		}

		OnCloseAction SetProtect(ProcessRecord.ProcessInterop.MEMORY_BASIC_INFORMATION memInfo, bool write)
		{
			Action action = () => { };

			var protect = memInfo.Protect;
			if ((protect & ProcessRecord.ProcessInterop.PageProtect.PAGE_GUARD) != 0)
				protect ^= ProcessRecord.ProcessInterop.PageProtect.PAGE_GUARD;
			var extra = protect & (ProcessRecord.ProcessInterop.PageProtect.PAGE_GUARD - 1);
			if (write)
			{
				if ((protect & (ProcessRecord.ProcessInterop.PageProtect.PAGE_EXECUTE | ProcessRecord.ProcessInterop.PageProtect.PAGE_EXECUTE_READ | ProcessRecord.ProcessInterop.PageProtect.PAGE_EXECUTE_WRITECOPY)) != 0)
					protect = ProcessRecord.ProcessInterop.PageProtect.PAGE_EXECUTE_READWRITE;
				if ((protect & (ProcessRecord.ProcessInterop.PageProtect.PAGE_NOACCESS | ProcessRecord.ProcessInterop.PageProtect.PAGE_READONLY | ProcessRecord.ProcessInterop.PageProtect.PAGE_WRITECOPY)) != 0)
					protect = ProcessRecord.ProcessInterop.PageProtect.PAGE_READWRITE;
			}
			else
			{
				if ((protect & ProcessRecord.ProcessInterop.PageProtect.PAGE_NOACCESS) != 0)
					protect = ProcessRecord.ProcessInterop.PageProtect.PAGE_READONLY;
			}
			protect |= extra;

			// Can't change protection on mapped memory
			if ((memInfo.Type & ProcessRecord.ProcessInterop.MemType.MEM_MAPPED) != 0)
				protect = memInfo.Protect;

			if (memInfo.Protect != protect)
			{
				ProcessRecord.ProcessInterop.PageProtect oldProtect;
				if (!ProcessRecord.ProcessInterop.VirtualProtectEx(handle, memInfo.BaseAddress, memInfo.RegionSize, protect, out oldProtect))
					throw new Win32Exception();
				action = () =>
				{
					if (!ProcessRecord.ProcessInterop.VirtualProtectEx(handle, memInfo.BaseAddress, memInfo.RegionSize, memInfo.Protect, out oldProtect))
						throw new Win32Exception();
					if (!ProcessRecord.ProcessInterop.FlushInstructionCache(handle, memInfo.BaseAddress, memInfo.RegionSize))
						throw new Win32Exception();
				};
			}

			return new OnCloseAction(action);
		}

		IBinaryDataChangedDelegate changed;
		public event IBinaryDataChangedDelegate Changed
		{
			add { changed += value; }
			remove { changed -= value; }
		}

		readonly int pid;
		public ProcessBinaryData(int PID)
		{
			pid = PID;
		}

		public bool CanInsert() { return false; }

		int suspendCount = 0;
		void SuspendProcess()
		{
			if (suspendCount++ != 0)
				return;

			ProcessRecord.SuspendProcess(pid);
		}

		void ResumeProcess()
		{
			if (--suspendCount != 0)
				return;

			ProcessRecord.ResumeProcess(pid);
		}

		IntPtr handle;
		int openCount = 0;
		void OpenProcess()
		{
			if (openCount++ != 0)
				return;

			handle = ProcessRecord.ProcessInterop.OpenProcess(ProcessRecord.ProcessInterop.ProcessAccessFlags.PROCESS_QUERY_INFORMATION | ProcessRecord.ProcessInterop.ProcessAccessFlags.PROCESS_VM_READ | ProcessRecord.ProcessInterop.ProcessAccessFlags.PROCESS_VM_WRITE | ProcessRecord.ProcessInterop.ProcessAccessFlags.PROCESS_VM_OPERATION, false, pid);
			if (handle == IntPtr.Zero)
				throw new Win32Exception();
		}

		void CloseProcess()
		{
			if (--openCount != 0)
				return;

			if (!ProcessRecord.ProcessInterop.CloseHandle(handle))
				throw new Win32Exception();
		}

		long cacheStart, cacheEnd;
		bool cacheHasData;
		byte[] cache = new byte[65536];
		void SetCache(long index, int count)
		{
			if ((index >= cacheStart) && (index + count <= cacheEnd))
				return;

			if (count > cache.Length)
				throw new ArgumentException("count");

			using (Suspend())
			using (Open())
			{
				cacheStart = cacheEnd = index;
				cacheHasData = false;

				while (cacheEnd - cacheStart < count)
				{
					bool hasData;
					ProcessRecord.ProcessInterop.MEMORY_BASIC_INFORMATION memInfo;
					if (ProcessRecord.ProcessInterop.VirtualQueryEx(handle, new IntPtr(index), out memInfo, Marshal.SizeOf(typeof(ProcessRecord.ProcessInterop.MEMORY_BASIC_INFORMATION))))
					{
						hasData = (memInfo.State & ProcessRecord.ProcessInterop.MemoryState.MEM_COMMIT) != 0;
						if ((hasData) && ((memInfo.Type & ProcessRecord.ProcessInterop.MemType.MEM_MAPPED) == 0))
						{
							if ((memInfo.Protect & ProcessRecord.ProcessInterop.PageProtect.PAGE_NOACCESS) != 0)
								hasData = false;
						}
						cacheEnd = memInfo.BaseAddress.ToInt64() + memInfo.RegionSize.ToInt64();
					}
					else
					{
						if (Marshal.GetLastWin32Error() != 87)
							throw new Win32Exception();

						hasData = false;
						cacheEnd = long.MaxValue;
					}

					if ((!hasData) && (!cacheHasData) && (cacheEnd - cacheStart >= count))
						return;

					cacheHasData = true;
					cacheEnd = Math.Min(cacheEnd, cacheStart + cache.Length);

					if (!hasData)
						Array.Clear(cache, (int)(index - cacheStart), (int)(cacheEnd - index));
					else
						using (SetProtect(memInfo, false))
						{
							var pin = GCHandle.Alloc(cache, GCHandleType.Pinned);
							try
							{
								var ptr = new IntPtr(pin.AddrOfPinnedObject().ToInt64() + index - cacheStart);
								IntPtr read;
								if (!ProcessRecord.ProcessInterop.ReadProcessMemory(handle, new IntPtr(index), ptr, (int)(cacheEnd - index), out read))
									throw new Win32Exception();
							}
							finally
							{
								pin.Free();
							}
						}

					index = cacheEnd;
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

			using (Suspend())
			using (Open())
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
			if (count != bytes.Length)
				throw new Exception("Cannot change byte count.");

			using (Suspend())
			using (Open())
			{
				while (bytes.Length > 0)
				{
					ProcessRecord.ProcessInterop.MEMORY_BASIC_INFORMATION memInfo;
					if (ProcessRecord.ProcessInterop.VirtualQueryEx(handle, new IntPtr(index), out memInfo, Marshal.SizeOf(typeof(ProcessRecord.ProcessInterop.MEMORY_BASIC_INFORMATION))))
					{
						if ((memInfo.State & ProcessRecord.ProcessInterop.MemoryState.MEM_COMMIT) == 0)
							throw new Exception("Cannot write to this memory");
					}
					else
					{
						if (Marshal.GetLastWin32Error() != 87)
							throw new Win32Exception();

						throw new Exception("Cannot write to this memory");
					}

					var end = memInfo.BaseAddress.ToInt64() + memInfo.RegionSize.ToInt64();
					var numBytes = (int)Math.Min(bytes.Length, end - index);

					using (SetProtect(memInfo, true))
					{
						IntPtr written;
						if (!ProcessRecord.ProcessInterop.WriteProcessMemory(handle, new IntPtr(index), bytes, numBytes, out written))
							throw new Win32Exception();
					}

					index += numBytes;
					bytes = bytes.Skip(numBytes).ToArray();
				}
			}

			Refresh();
			changed();
		}

		public void Refresh()
		{
			cacheStart = cacheEnd = 0;
			changed();
		}

		public byte[] GetAllBytes()
		{
			throw new NotImplementedException();
		}

		public byte[] GetSubset(long index, long count)
		{
			using (Suspend())
			{
				var result = new byte[count];
				SetCache(index, (int)count);
				if (cacheHasData)
					Array.Copy(cache, index - cacheStart, result, 0, count);
				return result;
			}
		}
	}
}
