using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32.SafeHandles;
using NeoEdit.Common;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.HexEdit.Data
{
	class ProcessBinaryData : BinaryData
	{
		class OnCloseAction : IDisposable
		{
			Action closeAction;
			public OnCloseAction(Action _closeAction) { closeAction = _closeAction; }

			public void Dispose() => closeAction();
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

		readonly int pid;
		public ProcessBinaryData(int pid)
		{
			this.pid = pid;

			using (Suspend())
			using (Open())
				Length = GetProcessMemoryLength();
		}

		int suspendCount = 0;
		void SuspendProcess()
		{
			if (suspendCount++ != 0)
				return;

			Process.GetProcessById(pid).Suspend();
		}

		void ResumeProcess()
		{
			if (--suspendCount != 0)
				return;

			Process.GetProcessById(pid).Resume();
		}

		SafeProcessHandle handle;
		int openCount = 0;
		void OpenProcess()
		{
			if (openCount++ != 0)
				return;

			handle = Win32.OpenProcess(Win32.ProcessAccessFlags.QueryInformation | Win32.ProcessAccessFlags.VirtualMemoryRead | Win32.ProcessAccessFlags.VirtualMemoryWrite | Win32.ProcessAccessFlags.VirtualMemoryOperation, false, pid);
			if (handle.IsInvalid)
				throw new Win32Exception();
		}

		void CloseProcess()
		{
			if (--openCount != 0)
				return;

			handle.Dispose();
		}

		protected override unsafe void VirtRead(long index, out byte[] block, out long blockStart, out long blockEnd)
		{
			using (Suspend())
			using (Open())
			{
				block = null;
				blockStart = blockEnd = index;

				var queryInfo = VirtualQuery(index);
				if (queryInfo == null)
				{
					blockEnd = Length;
					return;
				}

				blockEnd = queryInfo.EndAddress;

				if ((!queryInfo.Committed) || ((queryInfo.Mapped) && queryInfo.NoAccess))
				{
					blockStart = queryInfo.StartAddress;
					return;
				}

				blockEnd = Math.Min(blockEnd, blockStart + 65536);

				block = new byte[blockEnd - blockStart];

				using (SetProtect(queryInfo, false))
					fixed (byte* ptr = block)
					{
						IntPtr read;
						if (!Win32.ReadProcessMemory(handle.DangerousGetHandle(), (IntPtr)index, (IntPtr)(ptr + index - blockStart), (int)(blockEnd - index), out read))
							throw new Win32Exception();
					}
			}
		}

		public override bool Find(FindBinaryDialog.Result currentFind, long index, out long start, out long end, bool forward = true)
		{
			using (Suspend())
			using (Open())
				return base.Find(currentFind, index, out start, out end, forward);
		}

		protected override void VirtWrite(long index, long count, byte[] bytes)
		{
			using (Suspend())
			using (Open())
			{
				while (bytes.Length > 0)
				{
					var queryInfo = VirtualQuery(index);
					if ((queryInfo == null) || (!queryInfo.Committed))
						throw new Exception("Cannot write to this memory");

					var end = queryInfo.EndAddress;
					var numBytes = (int)Math.Min(bytes.Length, end - index);

					using (SetProtect(queryInfo, true))
					{
						IntPtr written;
						if (!Win32.WriteProcessMemory(handle.DangerousGetHandle(), (IntPtr)index, bytes, numBytes, out written))
							throw new Win32Exception();
					}

					index += numBytes;
					Array.Copy(bytes, numBytes, bytes, 0, bytes.Length - numBytes);
					Array.Resize(ref bytes, bytes.Length - numBytes);
				}
			}
		}

		public override void Save(string fileName)
		{
			using (Suspend())
			using (Open())
			using (var output = File.Create(fileName))
			{
				long sectionStart = -1, sectionEnd = -1;
				var start = new List<long>();
				var end = new List<long>();

				byte[] block = null;
				long blockStart = 0, blockEnd = 0;
				long index = 0;
				while (true)
				{
					block = null;
					if (index < Length)
						VirtRead(index, out block, out blockStart, out blockEnd);

					if (block != null)
					{
						if (sectionStart == -1)
							sectionStart = blockStart;
						sectionEnd = blockEnd;
						output.Write(block, 0, block.Length);
					}
					else if (sectionStart != -1)
					{
						start.Add(sectionStart);
						end.Add(sectionEnd);
						sectionStart = sectionEnd = -1;
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

					index = blockEnd;
				}
			}
		}

		long GetProcessMemoryLength()
		{
			long index = 0;
			while (true)
			{
				var result = VirtualQuery(index);
				if (result == null)
					return index;
				index = result.EndAddress;
			}
		}

		VirtualQueryInfo VirtualQuery(long index)
		{
			var max = UIntPtr.Size == 4 ? uint.MaxValue : ulong.MaxValue;
			if ((ulong)index >= max)
				return null;

			Win32.MEMORY_BASIC_INFORMATION memInfo;
			if (Win32.VirtualQueryEx(handle.DangerousGetHandle(), (IntPtr)index, out memInfo, Win32.MEMORY_BASIC_INFORMATION.Size) == 0)
			{
				if (Win32.LastError != Win32.ERROR_INVALID_PARAMETER)
					throw new Win32Exception();
				return null;
			}

			var info = new VirtualQueryInfo();
			info.Committed = (memInfo.State & Win32.MEM_COMMIT) != 0;
			info.Mapped = (memInfo.Type & Win32.MEM_MAPPED) != 0;
			info.NoAccess = (memInfo.Protect & Win32.PAGE_NOACCESS) != 0;
			info.StartAddress = memInfo.BaseAddress.ToInt64();
			info.RegionSize = (long)Math.Min((ulong)memInfo.RegionSize.ToInt64(), max - (ulong)memInfo.BaseAddress.ToInt64());
			info.EndAddress = memInfo.BaseAddress.ToInt64() + info.RegionSize;
			info.Protect = memInfo.Protect;
			return info;
		}

		Protect SetProtect(VirtualQueryInfo info, bool write)
		{
			var protect = info.Protect;

			if (!info.Mapped) // Can't change protection on mapped memory
			{
				if ((protect & Win32.PAGE_GUARD) != 0)
					protect ^= Win32.PAGE_GUARD;
				var extra = protect & ~(Win32.PAGE_GUARD - 1);
				if (write)
				{
					if ((protect & (Win32.PAGE_EXECUTE | Win32.PAGE_EXECUTE_READ | Win32.PAGE_EXECUTE_WRITECOPY)) != 0)
						protect = Win32.PAGE_EXECUTE_READWRITE;
					if ((protect & (Win32.PAGE_NOACCESS | Win32.PAGE_READONLY | Win32.PAGE_WRITECOPY)) != 0)
						protect = Win32.PAGE_READWRITE;
				}
				else
				{
					if ((protect & Win32.PAGE_NOACCESS) != 0)
						protect = Win32.PAGE_READONLY;
				}
				protect |= extra;
			}

			return new Protect(handle, info, protect);
		}
	}
}
