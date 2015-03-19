using System;
using System.IO;

namespace NeoEdit.HexEdit.Data
{
	class MemoryBinaryData : BinaryData
	{
		byte[] data;
		public MemoryBinaryData(byte[] _data = null)
		{
			data = _data ?? new byte[0];
			Length = data.Length;
		}

		protected override void VirtRead(long index, out byte[] block, out long blockStart, out long blockEnd)
		{
			block = data;
			blockStart = 0;
			blockEnd = data.Length;
		}

		public override bool CanInsert() { return true; }

		protected override void VirtWrite(long index, long count, byte[] bytes)
		{
			Array.Resize(ref data, data.Length + Math.Max(0, bytes.Length - (int)count));
			Array.Copy(data, index + count, data, index + bytes.Length, Length - index - count);
			Array.Resize(ref data, data.Length + Math.Min(0, bytes.Length - (int)count));
			Array.Copy(bytes, 0, data, index, bytes.Length);
			Length = data.Length;
		}

		public override byte[] GetAllBytes()
		{
			return data;
		}

		public override void Save(string filename)
		{
			File.WriteAllBytes(filename, data);
		}
	}
}
