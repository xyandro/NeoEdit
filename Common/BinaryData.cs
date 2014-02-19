using System;
using System.Linq;

namespace NeoEdit.Common
{
	public class BinaryData : IBinaryData
	{
		IBinaryDataChangedDelegate changed;
		public event IBinaryDataChangedDelegate Changed
		{
			add { changed += value; }
			remove { changed -= value; }
		}

		byte[] data;
		public BinaryData(byte[] _data)
		{
			data = _data;
		}

		public bool CanInsert() { return true; }
		public void Refresh() { }

		public byte this[long index]
		{
			get { return data[index]; }
		}

		public long Length
		{
			get { return data.Length; }
		}

		public bool Find(FindData currentFind, long index, out long start, out long end, bool forward = true)
		{
			start = end = -1;

			Func<byte[], long, byte[], bool, long> findFunc;
			Func<long, long, bool> compareFunc;
			if (forward)
			{
				++index;
				findFunc = Helpers.ForwardArraySearch;
				compareFunc = (a, b) => a < b;
			}
			else
			{
				--index;
				findFunc = Helpers.BackwardArraySearch;
				compareFunc = (a, b) => a > b;
			}

			for (var findPos = 0; findPos < currentFind.Data.Count; findPos++)
			{
				var found = findFunc(data, index, currentFind.Data[findPos], currentFind.IgnoreCase[findPos]);
				if ((found != -1) && ((start == -1) || (compareFunc(found, start))))
				{
					start = found;
					end = start + currentFind.Data[findPos].Length;
				}
			}

			return start != -1;
		}

		public void Replace(long index, long count, byte[] bytes)
		{
			if ((index < 0) || (index > data.Length))
				throw new ArgumentOutOfRangeException("offset");
			if ((count < 0) || (index + count > data.Length))
				throw new ArgumentOutOfRangeException("length");

			if (bytes == null)
				bytes = new byte[0];

			var newData = new byte[data.Length - count + bytes.Length];
			Array.Copy(data, 0, newData, 0, index);
			Array.Copy(bytes, 0, newData, index, bytes.Length);
			Array.Copy(data, index + count, newData, index + bytes.Length, data.Length - index - count);
			data = newData;
			changed();
		}

		public byte[] GetAllBytes()
		{
			return data;
		}

		public byte[] GetSubset(long index, long count)
		{
			index = Math.Max(0, Math.Min(data.Length - 1, index));
			count = Math.Max(0, Math.Min(data.Length - index, count));
			return data.Skip((int)index).Take((int)count).ToArray();
		}
	}
}
