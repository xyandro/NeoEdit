﻿using System;
using System.IO;

namespace NeoEdit.HexEdit.Data
{
	class MemoryBinaryData : BinaryData
	{
		public MemoryBinaryData(byte[] data = null)
		{
			if (data == null)
				data = new byte[0];
			cache = data;
			cacheStart = 0;
			cacheEnd = Length = data.Length;
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

			var cacheLen = cache.Length;
			Array.Resize(ref cache, cache.Length + Math.Max(0, bytes.Length - (int)count));
			Array.Copy(cache, index + count, cache, index + bytes.Length, cacheLen - index - count);
			Array.Resize(ref cache, cache.Length + Math.Min(0, bytes.Length - (int)count));
			Array.Copy(bytes, 0, cache, index, bytes.Length);
			cacheEnd = Length = cache.Length;
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
