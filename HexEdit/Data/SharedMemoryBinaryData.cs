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

		protected override void ReadBlock(long index, int count, out byte[] block, out long blockStart, out long blockEnd)
		{
			blockStart = index;
			blockEnd = Math.Min(index + count, Length);
			block = new byte[blockEnd - blockStart];
			Interop.ReadSharedMemory(pid, handle, blockStart, block, 0, block.Length);
		}

		public override void Replace(long index, long count, byte[] bytes)
		{
			if (count != bytes.Length)
				throw new Exception("Cannot change size.");

			var length = bytes.Length;
			Interop.WriteSharedMemory(pid, handle, index, bytes);

			Refresh();
		}
	}
}
