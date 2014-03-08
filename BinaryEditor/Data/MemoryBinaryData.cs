using System;
using System.IO;

namespace NeoEdit.BinaryEditor
{
	class MemoryBinaryData : BinaryData
	{
		public MemoryBinaryData(byte[] data = null)
		{
			if (data == null)
				data = new byte[0];
			cache = data;
			cacheStart = 0;
			cacheEnd = length = data.Length;
			cacheHasData = true;
		}

		public override bool CanInsert() { return true; }

		public override void Replace(long index, long count, byte[] bytes)
		{
			if ((index < 0) || (index > cache.Length))
				throw new ArgumentOutOfRangeException("offset");
			if ((count < 0) || (index + count > cache.Length))
				throw new ArgumentOutOfRangeException("length");

			if (bytes == null)
				bytes = new byte[0];

			var newData = new byte[cache.Length - count + bytes.Length];
			Array.Copy(cache, 0, newData, 0, index);
			Array.Copy(bytes, 0, newData, index, bytes.Length);
			Array.Copy(cache, index + count, newData, index + bytes.Length, cache.Length - index - count);
			cache = newData;
			length = cache.Length;
		}

		public override byte[] GetAllBytes()
		{
			return cache;
		}

		public override void Save(string filename)
		{
			File.WriteAllBytes(filename, cache);
		}
	}
}
