using System;
using NeoEdit.Common;
using NeoEdit.Interop;

namespace NeoEdit.Records.Handles
{
	public class SharedMemoryBinaryData : BinaryData
	{
		string name;
		long length;

		int GetPID()
		{
			return Convert.ToInt32(name.Substring(0, name.IndexOf("/")));
		}

		IntPtr GetHandle()
		{
			return (IntPtr)Convert.ToInt64(name.Substring(name.IndexOf("/") + 1));
		}

		public SharedMemoryBinaryData(string _name)
		{
			name = _name;
			length = NEInterop.GetSharedMemorySize(GetPID(), GetHandle());
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
			NEInterop.ReadSharedMemory(GetPID(), GetHandle(), (IntPtr)index, cache, (int)(index - cacheStart), (int)(cacheEnd - index));
		}

		public override void Replace(long index, long count, byte[] bytes)
		{
			if (count != bytes.Length)
				throw new Exception("Cannot change size.");

			var length = bytes.Length;
			NEInterop.WriteSharedMemory(GetPID(), GetHandle(), (IntPtr)index, bytes);

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
