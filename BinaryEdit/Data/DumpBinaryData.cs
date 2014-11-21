using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NeoEdit.BinaryEdit.Data
{
	class DumpBinaryData : BinaryData
	{
		Stream input;
		List<long> start = new List<long>();
		List<long> end = new List<long>();
		List<long> pos = new List<long>();
		public DumpBinaryData(string filename)
		{
			length = 0x80000000000;

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

		protected override void SetCache(long index, int count)
		{
			if ((index >= cacheStart) && (index + count <= cacheEnd))
				return;

			if (count > cache.Length)
				throw new ArgumentException("count");

			cacheStart = cacheEnd = index;
			cacheHasData = false;

			while (cacheEnd - cacheStart < count)
			{
				var idx = start.FindLastIndex(num => index >= num);
				var hasData = (idx != -1) && (index >= start[idx]) && (index < end[idx]);
				if (idx == -1)
					cacheEnd = start[0];
				else if (hasData)
					cacheEnd = end[idx];
				else if (idx + 1 >= start.Count)
					cacheEnd = Length;
				else
					cacheEnd = start[idx + 1];

				if ((!hasData) && (!cacheHasData) && (cacheEnd - cacheStart >= count))
					return;

				cacheHasData = true;
				cacheEnd = Math.Min(cacheEnd, cacheStart + cache.Length);

				if (!hasData)
					Array.Clear(cache, (int)(index - cacheStart), (int)(cacheEnd - index));
				else
				{
					input.Position = pos[idx] + index - start[idx];
					input.Read(cache, (int)(index - cacheStart), (int)(cacheEnd - index));
				}

				index = cacheEnd;
			}
		}

		public override void Refresh()
		{
			cacheStart = cacheEnd = 0;
		}
	}
}
