using System;
using NeoEdit.Common;
using NeoEdit.Interop;

namespace NeoEdit.Records.SharedMemory
{
	public class SharedMemoryBinaryData : BinaryData
	{
		string name;
		long length;
		public SharedMemoryBinaryData(string _name)
		{
			name = _name;
			length = NEInterop.GetSharedMemorySize(name);
		}

		public override long Length { get { return length; } }

		protected override void SetCache(long index, int count)
		{
			if ((index >= cacheStart) && (index + count <= cacheEnd))
				return;

			if (count > cache.Length)
				throw new ArgumentException("count");

			cacheStart = index;
			cacheEnd = Math.Min(cacheStart + cache.Length, Length);
			cacheHasData = true;
			NEInterop.ReadSharedMemory(name, (IntPtr)index, cache, (int)(index - cacheStart), (int)(cacheEnd - index));
		}

		public override void Replace(long index, long count, byte[] bytes)
		{
			if (count != bytes.Length)
				throw new Exception("Cannot change size.");

			var length = bytes.Length;
			NEInterop.WriteSharedMemory(name, (IntPtr)index, bytes);

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
