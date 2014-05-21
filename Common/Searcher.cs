using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Common
{
	public class Searcher
	{
		class FindData
		{
			public int[] CharMap { get; private set; }
			public int NumChars { get; private set; }

			public FindData(IEnumerable<String> strs)
			{
				CharMap = new int[Char.MaxValue];
				for (var ctr = 0; ctr < CharMap.Length; ++ctr) CharMap[ctr] = -1;
				foreach (var str in strs)
					foreach (var c in str)
					{
						if (CharMap[Char.ToLowerInvariant((char)c)] == -1)
							CharMap[Char.ToLowerInvariant((char)c)] = NumChars++;
						if (CharMap[Char.ToUpperInvariant((char)c)] == -1)
							CharMap[Char.ToUpperInvariant((char)c)] = NumChars++;
					}
			}

			public FindData(IEnumerable<Char[]> strs)
			{
				CharMap = new int[Char.MaxValue];
				for (var ctr = 0; ctr < CharMap.Length; ++ctr) CharMap[ctr] = -1;
				foreach (var str in strs)
					foreach (var c in str)
					{
						if (CharMap[Char.ToLowerInvariant((char)c)] == -1)
							CharMap[Char.ToLowerInvariant((char)c)] = NumChars++;
						if (CharMap[Char.ToUpperInvariant((char)c)] == -1)
							CharMap[Char.ToUpperInvariant((char)c)] = NumChars++;
					}
			}

			public FindData(IEnumerable<Byte[]> strs)
			{
				CharMap = new int[Byte.MaxValue];
				for (var ctr = 0; ctr < CharMap.Length; ++ctr) CharMap[ctr] = -1;
				foreach (var str in strs)
					foreach (var c in str)
					{
						if (CharMap[Char.ToLowerInvariant((char)c)] == -1)
							CharMap[Char.ToLowerInvariant((char)c)] = NumChars++;
						if (CharMap[Char.ToUpperInvariant((char)c)] == -1)
							CharMap[Char.ToUpperInvariant((char)c)] = NumChars++;
					}
			}
		}

		Searcher[] data;
		bool done;
		int length;
		FindData findData;

		Searcher(FindData findData)
		{
			data = new Searcher[findData.NumChars];
			this.findData = findData;
		}

		Searcher this[int mappedIndex]
		{
			get { return data[mappedIndex]; }
			set { data[mappedIndex] = value; }
		}

		public static Searcher Create(IEnumerable<String> strs, bool caseSensitive = true)
		{
			var findData = new FindData(strs);
			var result = new Searcher(findData);
			foreach (var str in strs)
			{
				var node = result;
				foreach (var c in str)
				{
					var mappedIndex = findData.CharMap[c];
					var mappedIndex2 = mappedIndex;
					if (!caseSensitive)
						if (Char.IsUpper((char)c))
							mappedIndex2 = findData.CharMap[Char.ToLowerInvariant((char)c)];
						else if (Char.IsLower((char)c))
							mappedIndex2 = findData.CharMap[Char.ToUpperInvariant((char)c)];
					if (node[mappedIndex] == null)
						node[mappedIndex] = node[mappedIndex2] = new Searcher(findData);
					node = node[mappedIndex];
				}
				node.done = true;
				node.length = str.Length;
			}
			return result;
		}

		public static Searcher Create(IEnumerable<Char[]> strs, bool caseSensitive = true)
		{
			var findData = new FindData(strs);
			var result = new Searcher(findData);
			foreach (var str in strs)
			{
				var node = result;
				foreach (var c in str)
				{
					var mappedIndex = findData.CharMap[c];
					var mappedIndex2 = mappedIndex;
					if (!caseSensitive)
						if (Char.IsUpper((char)c))
							mappedIndex2 = findData.CharMap[Char.ToLowerInvariant((char)c)];
						else if (Char.IsLower((char)c))
							mappedIndex2 = findData.CharMap[Char.ToUpperInvariant((char)c)];
					if (node[mappedIndex] == null)
						node[mappedIndex] = node[mappedIndex2] = new Searcher(findData);
					node = node[mappedIndex];
				}
				node.done = true;
				node.length = str.Count();
			}
			return result;
		}

		public static Searcher Create(IEnumerable<Byte[]> strs, bool caseSensitive = true)
		{
			var findData = new FindData(strs);
			var result = new Searcher(findData);
			foreach (var str in strs)
			{
				var node = result;
				foreach (var c in str)
				{
					var mappedIndex = findData.CharMap[c];
					var mappedIndex2 = mappedIndex;
					if (!caseSensitive)
						if (Char.IsUpper((char)c))
							mappedIndex2 = findData.CharMap[Char.ToLowerInvariant((char)c)];
						else if (Char.IsLower((char)c))
							mappedIndex2 = findData.CharMap[Char.ToUpperInvariant((char)c)];
					if (node[mappedIndex] == null)
						node[mappedIndex] = node[mappedIndex2] = new Searcher(findData);
					node = node[mappedIndex];
				}
				node.done = true;
				node.length = str.Count();
			}
			return result;
		}

		public List<Tuple<int, int>> Find(String input)
		{
			return Find(input, 0, input.Length);
		}

		public List<Tuple<int, int>> Find(Char[] input)
		{
			return Find(input, 0, input.Count());
		}

		public List<Tuple<int, int>> Find(Byte[] input)
		{
			return Find(input, 0, input.Count());
		}

		public List<Tuple<int, int>> Find(String input, int index, int length)
		{
			var result = new List<Tuple<int, int>>();
			length += index;
			var working = new List<Searcher>();
			for (var inputPos = index; inputPos < length; ++inputPos)
			{
				var mappedIndex = findData.CharMap[input[inputPos]];
				if (mappedIndex == -1)
				{
					working.Clear();
					continue;
				}

				working.Add(this);
				for (var ctr = 0; ctr < working.Count; )
				{
					if (working[ctr][mappedIndex] == null)
					{
						working.RemoveAt(ctr);
						continue;
					}

					working[ctr] = working[ctr][mappedIndex];
					if (working[ctr].done)
					{
						result.Add(new Tuple<int, int>(inputPos - working[ctr].length + 1, working[ctr].length));
						working.Clear();
						continue;
					}

					++ctr;
				}
			}
			return result;
		}

		public List<Tuple<int, int>> Find(Char[] input, int index, int length)
		{
			var result = new List<Tuple<int, int>>();
			length += index;
			var working = new List<Searcher>();
			for (var inputPos = index; inputPos < length; ++inputPos)
			{
				var mappedIndex = findData.CharMap[input[inputPos]];
				if (mappedIndex == -1)
				{
					working.Clear();
					continue;
				}

				working.Add(this);
				for (var ctr = 0; ctr < working.Count; )
				{
					if (working[ctr][mappedIndex] == null)
					{
						working.RemoveAt(ctr);
						continue;
					}

					working[ctr] = working[ctr][mappedIndex];
					if (working[ctr].done)
					{
						result.Add(new Tuple<int, int>(inputPos - working[ctr].length + 1, working[ctr].length));
						working.Clear();
						continue;
					}

					++ctr;
				}
			}
			return result;
		}

		public List<Tuple<int, int>> Find(Byte[] input, int index, int length)
		{
			var result = new List<Tuple<int, int>>();
			length += index;
			var working = new List<Searcher>();
			for (var inputPos = index; inputPos < length; ++inputPos)
			{
				var mappedIndex = findData.CharMap[input[inputPos]];
				if (mappedIndex == -1)
				{
					working.Clear();
					continue;
				}

				working.Add(this);
				for (var ctr = 0; ctr < working.Count; )
				{
					if (working[ctr][mappedIndex] == null)
					{
						working.RemoveAt(ctr);
						continue;
					}

					working[ctr] = working[ctr][mappedIndex];
					if (working[ctr].done)
					{
						result.Add(new Tuple<int, int>(inputPos - working[ctr].length + 1, working[ctr].length));
						working.Clear();
						continue;
					}

					++ctr;
				}
			}
			return result;
		}
	}
}
