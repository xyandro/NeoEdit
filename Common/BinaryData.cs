using System;
using System.Linq;
using System.Windows;

namespace NeoEdit.Common
{
	public class BinaryData
	{
		public delegate void ChangedDelegate();
		ChangedDelegate changed;
		public event ChangedDelegate Changed
		{
			add { changed += value; }
			remove { changed -= value; }
		}

		byte[] data;
		public BinaryData(byte[] _data)
		{
			data = _data;
		}

		public byte this[long index]
		{
			get { return data[index]; }
		}

		public long Length
		{
			get { return data.Length; }
		}

		public static implicit operator BinaryData(byte[] data)
		{
			return new BinaryData(data);
		}

		public byte[] Data { get { return data; } }

		public override string ToString()
		{
			return BitConverter.ToString(data);
		}

		public bool Find(FindData currentFind, long index, out long start, out long end, bool forward = true)
		{
			start = end = -1;
			var offset = forward ? 1 : -1;
			Func<byte, long, long> findFunc;
			if (forward)
			{
				findFunc = (_find, _start) =>
				{
					var _pos = Array.IndexOf(data, _find, (int)_start);
					if (_pos == -1)
						return long.MaxValue;
					return _pos;
				};
			}
			else
			{
				findFunc = (_find, _start) => Array.LastIndexOf(data, _find, (int)_start);
			}
			var selectFunc = forward ? (Func<long, long, long>)Math.Min : Math.Max;
			var invalid = forward ? long.MaxValue : -1;

			var pos = index;
			while (true)
			{
				pos += offset;
				if ((pos < 0) || (pos >= Length))
					return false;

				var usePos = invalid;
				for (var findPos = 0; findPos < currentFind.Data.Count; findPos++)
				{
					var ignoreCase = currentFind.IgnoreCase[findPos];
					var findData = currentFind.Data[findPos];

					usePos = selectFunc(usePos, findFunc(findData[0], pos));
					if (ignoreCase)
					{
						if ((findData[0] >= 'a') && (findData[0] <= 'z'))
							usePos = selectFunc(usePos, findFunc((byte)(findData[0] - 'a' + 'A'), pos));
						else if ((findData[0] >= 'A') && (findData[0] <= 'Z'))
							usePos = selectFunc(usePos, findFunc((byte)(findData[0] - 'A' + 'a'), pos));
					}
				}

				pos = usePos;
				if ((usePos < 0) || (usePos >= Length))
					return false;

				for (var findPos = 0; findPos < currentFind.Data.Count; findPos++)
				{
					var ignoreCase = currentFind.IgnoreCase[findPos];
					var findData = currentFind.Data[findPos];

					int findIdx;
					for (findIdx = 0; findIdx < findData.Length; ++findIdx)
					{
						if (pos + findIdx >= Length)
							break;

						if (data[pos + findIdx] == findData[findIdx])
							continue;

						if (!ignoreCase)
							break;

						if ((data[pos + findIdx] >= 'a') && (data[pos + findIdx] <= 'z') && (findData[findIdx] >= 'A') && (findData[findIdx] <= 'Z'))
							if (data[pos + findIdx] - 'a' + 'A' == findData[findIdx])
								continue;

						if ((data[pos + findIdx] >= 'A') && (data[pos + findIdx] <= 'Z') && (findData[findIdx] >= 'a') && (findData[findIdx] <= 'z'))
							if (data[pos + findIdx] - 'A' + 'a' == findData[findIdx])
								continue;

						break;
					}

					if (findIdx == findData.Length)
					{
						start = pos;
						end = pos + findData.Length - 1;
						return true;
					}
				}
			}
		}

		public void Replace(long offset, long length, byte[] bytes)
		{
			if ((offset < 0) || (offset > data.Length))
				throw new ArgumentOutOfRangeException("offset");
			if ((length < 0) || (offset + length > data.Length))
				throw new ArgumentOutOfRangeException("length");

			var newData = new byte[data.Length - length + bytes.Length];
			Array.Copy(data, 0, newData, 0, offset);
			Array.Copy(bytes, 0, newData, offset, bytes.Length);
			Array.Copy(data, offset + length, newData, offset + bytes.Length, data.Length - offset - length);
			data = newData;
			changed();
		}

		public BinaryData GetSubset(long index, long count)
		{
			index = Math.Max(0, Math.Min(data.Length - 1, index));
			count = Math.Max(0, Math.Min(data.Length - index, count));
			return data.Skip((int)index).Take((int)count).ToArray();
		}
	}
}
