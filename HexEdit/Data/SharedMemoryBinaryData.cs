using System;
using System.ComponentModel;
using Microsoft.Win32.SafeHandles;

namespace NeoEdit.HexEdit.Data
{
	class SharedMemoryBinaryData : BinaryData
	{
		int pid;
		IntPtr handle;
		public SharedMemoryBinaryData(int _pid, IntPtr _handle)
		{
			pid = _pid;
			handle = _handle;
			Length = GetSharedMemorySize(pid, handle);
		}

		protected override void VirtRead(long index, out byte[] block, out long blockStart, out long blockEnd)
		{
			blockStart = index;
			blockEnd = Math.Min(index + 65536, Length);
			block = new byte[blockEnd - blockStart];
			ReadSharedMemory(pid, handle, blockStart, block, 0, block.Length);
		}

		protected override void VirtWrite(long index, long count, byte[] bytes) => WriteSharedMemory(pid, handle, index, bytes);

		long GetSharedMemorySize(int pid, IntPtr handle)
		{
			using (var process = OpenProcess(pid))
			using (var dupHandle = DuplicateHandle(process, handle))
				return GetSizeOfMap(dupHandle.DangerousGetHandle());
		}

		unsafe void ReadSharedMemory(int pid, IntPtr handle, long index, byte[] bytes, int bytesIndex, int numBytes)
		{
			using (var process = OpenProcess(pid))
			using (var dupHandle = DuplicateHandle(process, handle))
			using (var ptr = Win32.MapViewOfFile(dupHandle.DangerousGetHandle(), Win32.FileMapAccess.FileMapRead, 0, 0, IntPtr.Zero))
			{
				if (ptr.IsInvalid)
					throw new Win32Exception();

				fixed (byte* bytesPtr = bytes)
					Win32.memcpy(bytesPtr + bytesIndex, (byte*)ptr.DangerousGetHandle() + index, numBytes);
			}
		}

		unsafe void WriteSharedMemory(int pid, IntPtr handle, long index, byte[] bytes)
		{
			using (var process = OpenProcess(pid))
			using (var dupHandle = DuplicateHandle(process, handle))
			using (var ptr = Win32.MapViewOfFile(dupHandle.DangerousGetHandle(), Win32.FileMapAccess.FileMapWrite, 0, 0, IntPtr.Zero))
			{
				if (ptr.IsInvalid)
					throw new Win32Exception();

				fixed (byte* bytesPtr = bytes)
					Win32.memcpy((byte*)ptr.DangerousGetHandle() + index, bytesPtr, bytes.Length);
			}
		}

		SafeProcessHandle OpenProcess(int pid, bool throwOnFail = true)
		{
			var handle = Win32.OpenProcess(Win32.ProcessAccessFlags.DuplicateHandle | Win32.ProcessAccessFlags.QueryInformation | Win32.ProcessAccessFlags.VirtualMemoryRead, false, pid);
			if (handle.IsInvalid)
				if (throwOnFail)
					throw new Win32Exception();
				else
					return null;
			return handle;
		}

		SafeProcessHandle DuplicateHandle(SafeProcessHandle process, IntPtr handle, bool throwOnFail = true)
		{
			SafeProcessHandle dupHandle;
			if (!Win32.DuplicateHandle(process, handle, Win32.GetCurrentProcess(), out dupHandle, 0, false, Win32.DUPLICATE_SAME_ACCESS))
				if (throwOnFail)
					throw new Win32Exception();
				else
					return null;
			return dupHandle;
		}

		long GetSizeOfMap(IntPtr handle)
		{
			using (var ptr = Win32.MapViewOfFile(handle, Win32.FileMapAccess.FileMapRead, 0, 0, IntPtr.Zero))
			{
				if (ptr.IsInvalid)
					throw new Win32Exception();

				Win32.MEMORY_BASIC_INFORMATION mbi;
				Win32.VirtualQuery(ptr.DangerousGetHandle(), out mbi, (IntPtr)Win32.MEMORY_BASIC_INFORMATION.Size);
				return mbi.RegionSize.ToInt64();
			}
		}
	}
}
