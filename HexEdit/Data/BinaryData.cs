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

		protected virtual void ReadBlock(long index, int count, out byte[] block, out long blockStart, out long blockEnd)
		{
			throw new NotImplementedException();
		}

		protected void SetCache(long index, int origCount)
		{
			if ((index >= cacheStart) && (index + origCount <= cacheEnd))
				return;

			var count = 65536;

			cache = new byte[count];
			cacheStart = index;
			cacheEnd = index + count;

			var read = 0;
			while (read < count)
			{
				byte[] block;
				long blockStart, blockEnd;
				ReadBlock(index + read, count - read, out block, out blockStart, out blockEnd);

				if (block != null)
					blockEnd = blockStart + block.Length;

				if ((index >= blockStart) && (index + origCount <= blockEnd))
				{
					cache = block;
					cacheStart = blockStart;
					cacheEnd = blockEnd;
					return;
				}

				var size = (int)Math.Min(blockEnd - index - read, count - read);
				if (block != null)
					Array.Copy(block, index + read - blockStart, cache, read, size);

				read += size;
			}
		}

		public byte this[long index]
		{
			get
			{
				SetCache(index, 1);
				if (cache == null)
					return 0;
				return cache[index - cacheStart];
			}
		}

		public long Length { get; protected set; }

		public virtual bool Find(BinaryFindDialog.Result currentFind, long index, out long start, out long end, bool forward = true)
		{
			start = end = -1;
			if (!forward)
				return false;

			++index;
			if ((index < 0) || (index >= Length))
				return false;

			var findLen = currentFind.Searcher.MaxLen;

			while (index < Length)
			{
				SetCache(index, findLen);
				if (cache != null)
				{
					var result = currentFind.Searcher.Find(cache, (int)(index - cacheStart), (int)(cache.LongLength - index + cacheStart), true);
					if (result.Count != 0)
					{
						start = result[0].Item1 + cacheStart;
						end = start + result[0].Item2;
						return true;
					}
				}

				index = cacheEnd;
				if (index != Length)
					index -= findLen - 1;
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
			var result = new byte[count];
			SetCache(index, (int)count);
			if (cache != null)
				Array.Copy(cache, index - cacheStart, result, 0, count);
			return result;
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
