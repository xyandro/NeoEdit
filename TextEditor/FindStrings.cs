using System;
using System.Collections.Generic;

namespace NeoEdit.TextEditor
{
	class FindStrings
	{
		class FindData
		{
			public int[] CharMap { get; private set; }
			public int NumChars { get; private set; }

			public FindData(IEnumerable<string> strs)
			{
				CharMap = new int[65536];
				for (var ctr = 0; ctr < CharMap.Length; ++ctr) CharMap[ctr] = -1;
				foreach (var str in strs)
					foreach (var c in str)
						if (CharMap[c] == -1)
							CharMap[c] = NumChars++;
			}
		}

		FindStrings[] data;
		bool done;
		int length;
		FindData findData;

		FindStrings(FindData findData)
		{
			data = new FindStrings[findData.NumChars];
			this.findData = findData;
		}

		FindStrings this[int mappedIndex]
		{
			get { return data[mappedIndex]; }
			set { data[mappedIndex] = value; }
		}

		public static FindStrings Create(IEnumerable<string> strs)
		{
			var findData = new FindData(strs);
			var result = new FindStrings(findData);
			foreach (var str in strs)
			{
				var node = result;
				foreach (var c in str)
				{
					var mappedIndex = findData.CharMap[c];
					if (node[mappedIndex] == null)
						node[mappedIndex] = new FindStrings(findData);
					node = node[mappedIndex];
				}
				node.done = true;
				node.length = str.Length;
			}
			return result;
		}

		public List<Tuple<int, int>> Find(string input, int index, int length)
		{
			var result = new List<Tuple<int, int>>();
			length += index;
			var working = new List<FindStrings>();
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
