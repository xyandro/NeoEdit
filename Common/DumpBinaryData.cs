using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NeoEdit.Common
{
	public class DumpBinaryData : IBinaryData
	{
		IBinaryDataChangedDelegate changed;
		public event IBinaryDataChangedDelegate Changed
		{
			add { changed += value; }
			remove { changed -= value; }
		}

		Stream input;
		List<long> start = new List<long>();
		List<long> end = new List<long>();
		List<long> pos = new List<long>();
		public DumpBinaryData(string filename)
		{
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

		public bool CanInsert() { return false; }

		long cacheStart, cacheEnd;
		bool cacheHasData;
		byte[] cache = new byte[65536];
		void SetCache(long index, int count)
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
					cacheEnd = long.MaxValue;
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

		public byte this[long index]
		{
			get
			{
				SetCache(index, 1);
				if (!cacheHasData)
					return 0;
				return cache[index - cacheStart];
			}
		}

		public long Length
		{
			get { return long.MaxValue; }
		}

		public bool Find(FindData currentFind, long index, out long start, out long end, bool forward = true)
		{
			start = end = -1;
			if (!forward)
				return false;

			++index;
			if ((index < 0) || (index >= Length))
				return false;

			var findLen = currentFind.Data.Select(bytes => bytes.Length).Max();

			while (index < Length)
			{
				SetCache(index, findLen);
				if (cacheHasData)
				{
					for (var findPos = 0; findPos < currentFind.Data.Count; findPos++)
					{
						var found = Helpers.ForwardArraySearch(cache, index - cacheStart, currentFind.Data[findPos], currentFind.IgnoreCase[findPos]);
						if ((found != -1) && ((start == -1) || (found < start)))
						{
							start = found + cacheStart;
							end = start + currentFind.Data[findPos].Length;
						}
					}

					if (start != -1)
						return true;
				}

				index = cacheEnd;
				if (index != long.MaxValue)
					index -= findLen - 1;
			}

			return false;
		}

		public void Replace(long index, long count, byte[] bytes)
		{
			throw new NotImplementedException();
		}

		public void Refresh()
		{
			cacheStart = cacheEnd = 0;
			changed();
		}

		public byte[] GetAllBytes()
		{
			throw new NotImplementedException();
		}

		public byte[] GetSubset(long index, long count)
		{
			var result = new byte[count];
			SetCache(index, (int)count);
			if (cacheHasData)
				Array.Copy(cache, index - cacheStart, result, 0, count);
			return result;
		}
	}
}
