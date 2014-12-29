using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NeoEdit.HexEdit.Data
{
	class DumpBinaryData : BinaryData
	{
		Stream input;
		List<long> start = new List<long>();
		List<long> end = new List<long>();
		List<long> pos = new List<long>();
		public DumpBinaryData(string filename)
		{
			Length = 0x80000000000;

			input = File.OpenRead(filename);

			var countBytes = new byte[sizeof(int)];
			input.Position = input.Length - countBytes.Length;
			input.Read(countBytes, 0, countBytes.Length);
			var count = BitConverter.ToInt32(countBytes, 0);

			input.Position = input.Length - sizeof(int) - sizeof(long) * count * 2;
			var posBytes = new byte[sizeof(long)];
			long curPos = 0;
			for (var ctr = 0; ctr < count; ++ctr)
			{
				input.Read(posBytes, 0, posBytes.Length);
				start.Add(BitConverter.ToInt64(posBytes, 0));
				input.Read(posBytes, 0, posBytes.Length);
				end.Add(BitConverter.ToInt64(posBytes, 0));
				pos.Add(curPos);
				curPos += end.Last() - start.Last();
			}
		}

		protected override void ReadBlock(long index, int count, out byte[] block, out long blockStart, out long blockEnd)
		{
			block = null;
			blockStart = blockEnd = index;

			var idx = start.FindLastIndex(num => index >= num);
			var hasData = (idx != -1) && (index >= start[idx]) && (index < end[idx]);
			if (idx == -1)
				blockEnd = start[0];
			else if (hasData)
				blockEnd = end[idx];
			else if (idx + 1 >= start.Count)
				blockEnd = Length;
			else
				blockEnd = start[idx + 1];

			if (!hasData)
				return;

			blockEnd = Math.Min(blockEnd, index + count);
			block = new byte[blockEnd - index];

			input.Position = pos[idx] + index - start[idx];
			input.Read(block, 0, block.Length);
		}
	}
}
