using System;
using NeoEdit.GUI.Common;
using NeoEdit.Interop;

namespace NeoEdit.GUI.Records.Handles
{
	public class SharedMemoryBinaryData : BinaryData
	{
		int pid;
		IntPtr handle;
		public SharedMemoryBinaryData(int _pid, IntPtr _handle)
		{
			pid = _pid;
			handle = _handle;
			length = NEInterop.GetSharedMemorySize(pid, handle);
		}

		protected override void SetCache(long index, int count)
		{
			if ((index >= cacheStart) && (index + count <= cacheEnd))
				return;

			if (count > cache.Length)
				throw new ArgumentException("count");

			cacheStart = index;
			cacheEnd = Math.Min(cacheStart + cache.Length, Length);
			cacheHasData = true;
			NEInterop.ReadSharedMemory(pid, handle, (IntPtr)index, cache, (int)(index - cacheStart), (int)(cacheEnd - index));
		}

		public override void Replace(long index, long count, byte[] bytes)
		{
			if (count != bytes.Length)
				throw new Exception("Cannot change size.");

			var length = bytes.Length;
			NEInterop.WriteSharedMemory(pid, handle, (IntPtr)index, bytes);

			Refresh();
			changed();
		}

		public override void Refresh()
		{
			cacheStart = cacheEnd = 0;
			base.Refresh();
		}
	}
}
