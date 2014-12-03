﻿using System;
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
			length = Interop.GetSharedMemorySize(pid, handle);
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
			Interop.ReadSharedMemory(pid, handle, index, cache, (int)(index - cacheStart), (int)(cacheEnd - index));
		}

		public override void Replace(long index, long count, byte[] bytes)
		{
			if (count != bytes.Length)
				throw new Exception("Cannot change size.");

			var length = bytes.Length;
			Interop.WriteSharedMemory(pid, handle, index, bytes);

			Refresh();
		}

		public override void Refresh()
		{
			cacheStart = cacheEnd = 0;
			base.Refresh();
		}
	}
}