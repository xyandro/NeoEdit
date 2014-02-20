﻿using System;

namespace NeoEdit.Common
{
	public class MemoryBinaryData : BinaryData
	{
		public MemoryBinaryData(byte[] data)
		{
			cache = data;
			cacheStart = 0;
			cacheEnd = data.Length;
			cacheHasData = true;
		}

		public override bool CanInsert() { return true; }

		public override long Length { get { return cache.Length; } }

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
			changed();
		}

		public override byte[] GetAllBytes()
		{
			return cache;
		}
	}
}
