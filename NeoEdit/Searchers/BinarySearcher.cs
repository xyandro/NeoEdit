using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Program.Searchers
{
	public class BinarySearcher
	{
		const int NOMATCH = -1;

		static void GetLowerUpper(byte b, bool matchCase, out byte lower, out byte upper)
		{
			lower = upper = b;
			if (!matchCase)
			{
				if ((lower >= 'A') && (lower <= 'Z'))
					lower = (byte)(lower - 'A' + 'a');
				if ((upper >= 'a') && (upper <= 'z'))
					upper = (byte)(upper - 'a' + 'A');
			}
		}

		class CharMap
		{
			public readonly bool[] exists;
			readonly bool matchCase;

			public CharMap(bool matchCase)
			{
				this.matchCase = matchCase;
				exists = new bool[byte.MaxValue + 1];
			}

			public void Add(byte[] bytes)
			{
				if (bytes.Length == 0)
					return;
				for (var ctr = 0; ctr < bytes.Length; ++ctr)
				{
					BinarySearcher.GetLowerUpper(bytes[ctr], matchCase, out var lower, out var upper);
					exists[lower] = exists[upper] = true;
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
				data = new SearchNode[byte.MaxValue + 1];
				this.charMap = charMap;
			}

			public void Add(byte[] bytes, bool matchCase)
			{
				var node = this;
				for (var ctr = 0; ctr < bytes.Length; ++ctr)
				{
					var b = bytes[ctr];
					if (node.data[b] == null)
					{
						GetLowerUpper(b, matchCase, out var lower, out var upper);
						node.data[lower] = node.data[upper] = new SearchNode(charMap);
					}
					node = node.data[b];
				}
				node.length = bytes.Length;
			}
		}

		readonly SearchNode matchCaseStartNode, ignoreCaseStartNode;
		public readonly int MaxLen = 0;

		public BinarySearcher(IEnumerable<byte[]> findBytes, bool matchCase = false) : this(findBytes.Select(bytes => (bytes, matchCase)).ToList()) { }

		public BinarySearcher(List<(byte[], bool)> findBytes)
		{
			var matchCaseCharMap = new CharMap(true);
			var ignoreCaseCharMap = new CharMap(false);
			for (var ctr = 0; ctr < findBytes.Count; ++ctr)
			{
				var node = findBytes[ctr].Item2 ? matchCaseCharMap : ignoreCaseCharMap;
				node.Add(findBytes[ctr].Item1);
				MaxLen = Math.Max(MaxLen, findBytes[ctr].Item1.Length);
			}

			matchCaseStartNode = new SearchNode(matchCaseCharMap);
			ignoreCaseStartNode = new SearchNode(ignoreCaseCharMap);

			for (var ctr = 0; ctr < findBytes.Count; ++ctr)
			{
				var node = findBytes[ctr].Item2 ? matchCaseStartNode : ignoreCaseStartNode;
				node.Add(findBytes[ctr].Item1, findBytes[ctr].Item2);
			}
		}

		public bool Find(byte[] input) => Find(input, 0, input.Length);

		public bool Find(byte[] input, int startIndex, int length)
		{
			var endIndex = startIndex + length;
			var curNodes = new List<SearchNode>();
			for (var index = startIndex; index < endIndex; ++index)
			{
				var found = false;

				if (matchCaseStartNode.charMap.exists[input[index]])
				{
					curNodes.Add(matchCaseStartNode);
					found = true;
				}

				if (ignoreCaseStartNode.charMap.exists[input[index]])
				{
					curNodes.Add(ignoreCaseStartNode);
					found = true;
				}

				// Quick check: if the current byte doesn't appear in the search at all, skip everything and go on
				if (!found)
				{
					curNodes.Clear();
					continue;
				}

				var newNodes = new List<SearchNode>();
				for (var ctr = 0; ctr < curNodes.Count; ++ctr)
				{
					var newNode = curNodes[ctr].data[input[index]];
					if (newNode == null)
						continue;

					newNodes.Add(newNode);

					if (newNode.length != NOMATCH)
						return true;
				}
				curNodes = newNodes;
			}

			return false;
		}
	}
}
