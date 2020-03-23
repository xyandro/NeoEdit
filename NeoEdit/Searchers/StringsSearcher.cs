using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Program.Searchers
{
	public class StringsSearcher : ISearcher
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
			public readonly bool start;

			public SearchNode(CharMap charMap, bool start)
			{
				data = new SearchNode[charMap.numChars];
				this.charMap = charMap;
				this.start = start;
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
						node.data[index] = new SearchNode(charMap, false);
					node = node.data[index];
				}
				node.length = str.Length;
			}
		}

		readonly SearchNode matchCaseStartNode, ignoreCaseStartNode;
		readonly bool wholeWords, entireSelection, firstMatchOnly;

		public StringsSearcher(IEnumerable<string> findStrs, bool wholeWords = false, bool matchCase = false, bool entireSelection = false, bool firstMatchOnly = false) : this(findStrs.Select(str => (str, matchCase)).ToList(), wholeWords, entireSelection, firstMatchOnly) { }

		public StringsSearcher(List<(string, bool)> findStrs, bool wholeWords = false, bool entireSelection = false, bool firstMatchOnly = false)
		{
			this.wholeWords = wholeWords;
			this.entireSelection = entireSelection;
			this.firstMatchOnly = firstMatchOnly;

			var matchCaseCharMap = new CharMap(true);
			var ignoreCaseCharMap = new CharMap(false);
			for (var ctr = 0; ctr < findStrs.Count; ++ctr)
			{
				var node = findStrs[ctr].Item2 ? matchCaseCharMap : ignoreCaseCharMap;
				node.Add(findStrs[ctr].Item1);
			}

			matchCaseStartNode = new SearchNode(matchCaseCharMap, true);
			ignoreCaseStartNode = new SearchNode(ignoreCaseCharMap, true);

			for (var ctr = 0; ctr < findStrs.Count; ++ctr)
			{
				var node = findStrs[ctr].Item2 ? matchCaseStartNode : ignoreCaseStartNode;
				node.Add(findStrs[ctr].Item1);
			}
		}

		public List<Range> Find(string input, int addOffset = 0)
		{
			var result = new List<Range>();
			var curNodes = new List<SearchNode>();
			var startNodes = new SearchNode[] { matchCaseStartNode, ignoreCaseStartNode };
			for (var index = 0; index < input.Length; ++index)
			{
				var found = false;

				foreach (var node in startNodes)
				{
					if (node.charMap.index[input[index]] != -1)
					{
						if ((!entireSelection) || (index == 0))
							curNodes.Add(node);
						found = true;
					}
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

					if ((wholeWords) && (node.start) && (!Helpers.IsWordBoundary(input, index)))
						continue;

					newNodes.Add(newNode);

					if (newNode.length != NOMATCH)
					{
						if ((wholeWords) && (!Helpers.IsWordBoundary(input, index + 1)))
							continue;
						if ((entireSelection) && (index + 1 != input.Length))
							continue;
						result.Add(Range.FromIndex(index - newNode.length + 1 + addOffset, newNode.length));
						if (firstMatchOnly)
							return result;
					}
				}
				curNodes = newNodes;

				// If we don't have any nodes, we won't get any
				if ((entireSelection) && (curNodes.Count == 0))
					break;
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
