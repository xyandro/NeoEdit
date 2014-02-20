using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using NeoEdit.Common;

namespace NeoEdit.Records.Processes
{
	public class ProcessBinaryData : BinaryData
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
			var extra = protect & ~(ProcessRecord.ProcessInterop.PageProtect.PAGE_GUARD - 1);
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

		readonly int pid;
		public ProcessBinaryData(int PID)
		{
			pid = PID;
		}

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

		public override long Length { get { return 0x80000000000; } }

		protected override void SetCache(long index, int count)
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
						if ((hasData) && ((memInfo.Type & ProcessRecord.ProcessInterop.MemType.MEM_MAPPED) != 0))
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
						cacheEnd = Length;
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

		public override bool Find(FindData currentFind, long index, out long start, out long end, bool forward = true)
		{
			using (Suspend())
			using (Open())
				return base.Find(currentFind, index, out start, out end, forward);
		}

		public override void Replace(long index, long count, byte[] bytes)
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
					Array.Copy(bytes, numBytes, bytes, 0, bytes.Length - numBytes);
					Array.Resize(ref bytes, bytes.Length - numBytes);
				}
			}

			Refresh();
			changed();
		}

		public override void Refresh()
		{
			cacheStart = cacheEnd = 0;
			base.Refresh();
		}

		public void Save(string fileName)
		{
			using (Suspend())
			using (Open())
			using (var output = File.Create(fileName))
			{
				long startBlock = -1, endBlock = -1;
				var start = new List<long>();
				var end = new List<long>();

				cacheStart = cacheEnd = 0;
				long index = 0;
				while (true)
				{
					var hasData = false;
					if (index < Length)
					{
						SetCache(index, 1);
						hasData = cacheHasData;
					}

					if (hasData)
					{
						if (startBlock == -1)
							startBlock = cacheStart;
						endBlock = cacheEnd;
						output.Write(cache, 0, (int)(cacheEnd - cacheStart));
					}
					else if (startBlock != -1)
					{
						start.Add(startBlock);
						end.Add(endBlock);
						startBlock = endBlock = -1;
					}
					else if (index >= Length)
					{
						for (var ctr = 0; ctr < start.Count; ++ctr)
						{
							var startBytes = BitConverter.GetBytes(start[ctr]);
							output.Write(startBytes, 0, startBytes.Length);
							var endBytes = BitConverter.GetBytes(end[ctr]);
							output.Write(endBytes, 0, endBytes.Length);
						}
						var countBytes = BitConverter.GetBytes(start.Count);
						output.Write(countBytes, 0, countBytes.Length);
						break;
					}

					index = cacheEnd;
				}
			}
		}
	}
}
