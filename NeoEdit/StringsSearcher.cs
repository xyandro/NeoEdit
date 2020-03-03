using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Program
{
	public class StringsSearcher
	{
		const int NOMATCH = -1;

		class CharMap
		{
			public readonly int[] index;
			public int numChars;
			readonly bool matchCase;

			public CharMap(bool matchCase)
			{
				this.matchCase = matchCase;
				index = new int[char.MaxValue + 1];
				for (var ctr = 0; ctr < index.Length; ++ctr)
					index[ctr] = -1;
			}

			public void Add(string str)
			{
				for (var ctr = 0; ctr < str.Length; ++ctr)
				{
					var ch = str[ctr];
					if (index[ch] == -1)
						index[matchCase ? ch : char.ToLower(ch)] = index[matchCase ? ch : char.ToUpper(ch)] = numChars++;
				}
			}
		}

		class SearchNode
		{
			public readonly SearchNode[] data;
			public int length = NOMATCH;
			public readonly CharMap charMap;

			public SearchNode(CharMap charMap)
			{
				data = new SearchNode[charMap.numChars];
				this.charMap = charMap;
			}

			public void Add(string str)
			{
				if (str.Length == 0)
					return;
				var node = this;
				for (var ctr = 0; ctr < str.Length; ++ctr)
				{
					var index = charMap.index[str[ctr]];
					if (node.data[index] == null)
						node.data[index] = new SearchNode(charMap);
					node = node.data[index];
				}
				node.length = str.Length;
			}
		}

		readonly SearchNode matchCaseStartNode, ignoreCaseStartNode;
		readonly bool firstMatchOnly;

		public StringsSearcher(IEnumerable<string> findStrs, bool matchCase = false, bool firstMatchOnly = false) : this(findStrs.Select(str => (str, matchCase)).ToList(), firstMatchOnly) { }

		public StringsSearcher(List<(string, bool)> findStrs, bool firstMatchOnly = false)
		{
			this.firstMatchOnly = firstMatchOnly;

			var matchCaseCharMap = new CharMap(true);
			var ignoreCaseCharMap = new CharMap(false);
			for (var ctr = 0; ctr < findStrs.Count; ++ctr)
			{
				var node = findStrs[ctr].Item2 ? matchCaseCharMap : ignoreCaseCharMap;
				node.Add(findStrs[ctr].Item1);
			}

			matchCaseStartNode = new SearchNode(matchCaseCharMap);
			ignoreCaseStartNode = new SearchNode(ignoreCaseCharMap);

			for (var ctr = 0; ctr < findStrs.Count; ++ctr)
			{
				var node = findStrs[ctr].Item2 ? matchCaseStartNode : ignoreCaseStartNode;
				node.Add(findStrs[ctr].Item1);
			}
		}

		public List<Range> Find(string input, bool firstMatchOnly = false) => Find(input, 0, input.Length);

		public List<Range> Find(string input, int startIndex, int length)
		{
			var result = new List<Range>();
			var endIndex = startIndex + length;
			var curNodes = new List<SearchNode>();
			for (var index = startIndex; index < endIndex; ++index)
			{
				var found = false;

				if (matchCaseStartNode.charMap.index[input[index]] != -1)
				{
					curNodes.Add(matchCaseStartNode);
					found = true;
				}

				if (ignoreCaseStartNode.charMap.index[input[index]] != -1)
				{
					curNodes.Add(ignoreCaseStartNode);
					found = true;
				}

				// Quick check: if the current char doesn't appear in the search at all, skip everything and go on
				if (!found)
				{
					curNodes.Clear();
					continue;
				}

				var newNodes = new List<SearchNode>();
				for (var ctr = 0; ctr < curNodes.Count; ++ctr)
				{
					var node = curNodes[ctr];
					var mappedIndex = node.charMap.index[input[index]];
					if (mappedIndex == -1)
						continue;
					var newNode = node.data[mappedIndex];
					if (newNode == null)
						continue;

					newNodes.Add(newNode);

					if (newNode.length != NOMATCH)
					{
						result.Add(Range.FromIndex(index - newNode.length + 1, newNode.length));
						if (firstMatchOnly)
							return result;
					}
				}
				curNodes = newNodes;
			}

			// Take longest values
			result = result.GroupBy(value => value.Start).Select(group => group.OrderByDescending(value => value.Length).First()).OrderBy(value => value.Start).ToList();

			// Remove overlapping values
			for (var index = 0; index < result.Count;)
			{
				if ((index == 0) || (result[index].Start >= result[index - 1].Start + result[index - 1].Length))
					++index;
				else
					result.RemoveAt(index);
			}

			return result;
		}
	}
}
