using System;
using NeoEdit.Win32;

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
			Length = Interop.GetSharedMemorySize(pid, handle);
		}

		protected override void VirtRead(long index, out byte[] block, out long blockStart, out long blockEnd)
		{
			blockStart = index;
			blockEnd = Math.Min(index + 65536, Length);
			block = new byte[blockEnd - blockStart];
			Interop.ReadSharedMemory(pid, handle, blockStart, block, 0, block.Length);
		}

		protected override void VirtWrite(long index, long count, byte[] bytes)
		{
			Interop.WriteSharedMemory(pid, handle, index, bytes);
		}
	}
}
