using System;
using System.IO;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.HexEdit
{
	public class BinaryData
	{
		byte[] data;
		public byte[] Data { get { return data; } private set { data = value; } }
		public long Length { get; private set; }

		public BinaryData(byte[] data = null)
		{
			Data = data ?? new byte[0];
			Length = Data.Length;
		}

		public byte[] Read(long index, long count)
		{
			if ((index < 0) || (index + count > Length))
				throw new IndexOutOfRangeException();

			var result = new byte[count];
			Array.Copy(data, index, result, 0, count);
			return result;
		}

		public byte this[long index] => data[index];

		public bool Find(FindBinaryDialog.Result currentFind, long index, out long start, out long end, bool forward = true)
		{
			start = end = -1;
			if (!forward)
				return false;

			++index;
			if ((index < 0) || (index >= Length))
				return false;

			var min = index;
			var findLen = currentFind.Searcher.MaxLen;

			while (index < Length)
			{
				index = Math.Max(min, index - findLen - 1);
				var block = Read(index, Math.Min(65536, Length - index));
				var result = currentFind.Searcher.Find(block, 0, block.Length, true);
				if (result.Count != 0)
				{
					start = result[0].Item1 + index;
					end = start + result[0].Item2;
					return true;
				}

				index += block.Length;
			}

			return false;
		}

		public void Replace(long index, long count, byte[] bytes)
		{
			if ((index < 0) || (count < 0) || (index + count > Length))
				throw new ArgumentOutOfRangeException();
			bytes = bytes ?? new byte[0];

			Array.Resize(ref data, data.Length + Math.Max(0, bytes.Length - (int)count));
			Array.Copy(data, index + count, data, index + bytes.Length, Length - index - count);
			Array.Resize(ref data, data.Length + Math.Min(0, bytes.Length - (int)count));
			Array.Copy(bytes, 0, data, index, bytes.Length);
			Length = data.Length;
		}

		public Coder.CodePage CodePageFromBOM() => Coder.CodePageFromBOM(Read(0, Math.Min(10, Length)));

		public void Save(string filename) => File.WriteAllBytes(filename, data);
	}
}
