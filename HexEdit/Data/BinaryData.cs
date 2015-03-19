using System;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.HexEdit.Data
{
	public abstract class BinaryData
	{
		public virtual bool CanInsert() { return false; }

		byte[] cache = null;
		long cacheStart = -1, cacheEnd = -1;

		protected abstract void ReadBlock(long index, out byte[] block, out long blockStart, out long blockEnd);

		void SetCache(long index)
		{
			if ((index >= cacheStart) && (index < cacheEnd))
				return;

			ReadBlock(index, out cache, out cacheStart, out cacheEnd);
			if ((cache != null) && (cacheEnd != cacheStart + cache.Length))
				throw new Exception("Invalid newCacheEnd");
			if ((cacheEnd <= index) || (cacheStart > index))
				throw new Exception("Invalid cache");
		}

		public byte[] Read(long index, long count)
		{
			var data = new byte[count];
			while (count > 0)
			{
				SetCache(index);
				var size = Math.Min(count, cacheEnd - index);
				if (cache != null)
					Array.Copy(cache, index - cacheStart, data, data.Length - count, size);
				index += size;
				count -= size;
			}
			return data;
		}

		public long GetNonSparse(long index)
		{
			SetCache(index);
			if (cache != null)
				return index;
			return cacheEnd;
		}

		public byte this[long index] { get { return Read(index, 1)[0]; } }

		public long Length { get; protected set; }

		public virtual bool Find(BinaryFindDialog.Result currentFind, long index, out long start, out long end, bool forward = true)
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
				var nonSparse = GetNonSparse(index);
				if (nonSparse != index)
				{
					index = nonSparse;
					continue;
				}

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

		public virtual void Replace(long index, long count, byte[] bytes)
		{
			throw new NotImplementedException();
		}

		public void Refresh()
		{
			cache = null;
			cacheStart = cacheEnd = -1;
		}

		public virtual byte[] GetAllBytes()
		{
			throw new NotImplementedException();
		}

		public virtual byte[] GetSubset(long index, long count)
		{
			return Read(index, count);
		}

		public Coder.CodePage CodePageFromBOM()
		{
			return Coder.CodePageFromBOM(GetSubset(0, Math.Min(10, Length)));
		}

		public virtual void Save(string filename)
		{
			throw new NotImplementedException();
		}
	}
}
