﻿using System;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.HexEdit.Data
{
	public abstract class BinaryData
	{
		public virtual bool CanInsert() { return false; }

		protected long length = 0, cacheStart = 0, cacheEnd = 0;
		protected bool cacheHasData = false;
		protected byte[] cache = new byte[65536];
		protected virtual void SetCache(long index, int count) { }

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

		public long Length { get { return length; } }

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
				if (cacheHasData)
				{
					var result = currentFind.Searcher.Find(cache, (int)index, (int)(cache.LongLength - index), true);
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

		public virtual void Refresh() { }

		public virtual byte[] GetAllBytes()
		{
			throw new NotImplementedException();
		}

		public virtual byte[] GetSubset(long index, long count)
		{
			var result = new byte[count];
			SetCache(index, (int)count);
			if (cacheHasData)
				Array.Copy(cache, index - cacheStart, result, 0, count);
			return result;
		}

		public Coder.CodePage CodePageFromBOM()
		{
			return Coder.CodePageFromBOM(GetSubset(0, Math.Min(10, length)));
		}

		public virtual void Save(string filename)
		{
			throw new NotImplementedException();
		}
	}
}