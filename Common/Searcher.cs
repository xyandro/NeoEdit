using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Common
{
	static class SearcherExtensions
	{
		public static Char ToggleCase(this Char c)
		{
			if ((c >= 'a') && (c <= 'z'))
				return (Char)(c - 'a' + 'A');
			if ((c >= 'A') && (c <= 'Z'))
				return (Char)(c - 'A' + 'a');
			return c;
		}
		public static Byte ToggleCase(this Byte c)
		{
			if ((c >= 'a') && (c <= 'z'))
				return (Byte)(c - 'a' + 'A');
			if ((c >= 'A') && (c <= 'Z'))
				return (Byte)(c - 'A' + 'a');
			return c;
		}
	}

	public class Searcher
	{
		class FindData
		{
			public int[] CharMap { get; private set; }
			public int NumChars { get; private set; }
			readonly bool matchCase;

			public FindData(String str, bool matchCase)
			{
				this.matchCase = matchCase;
				CharMap = new int[Char.MaxValue + 1];
				for (var ctr = 0; ctr < CharMap.Length; ++ctr)
					CharMap[ctr] = -1;
			}

			public FindData(Char[] str, bool matchCase)
			{
				this.matchCase = matchCase;
				CharMap = new int[Char.MaxValue + 1];
				for (var ctr = 0; ctr < CharMap.Length; ++ctr)
					CharMap[ctr] = -1;
			}

			public FindData(Byte[] str, bool matchCase)
			{
				this.matchCase = matchCase;
				CharMap = new int[Byte.MaxValue + 1];
				for (var ctr = 0; ctr < CharMap.Length; ++ctr)
					CharMap[ctr] = -1;
			}

			public void Add(String str)
			{
				foreach (var c1 in str)
				{
					if (CharMap[c1] != -1)
						continue;

					var c2 = matchCase ? c1 : c1.ToggleCase();
					CharMap[c1] = CharMap[c2] = NumChars++;
				}
			}

			public void Add(Char[] str)
			{
				foreach (var c1 in str)
				{
					if (CharMap[c1] != -1)
						continue;

					var c2 = matchCase ? c1 : c1.ToggleCase();
					CharMap[c1] = CharMap[c2] = NumChars++;
				}
			}

			public void Add(Byte[] str)
			{
				foreach (var c1 in str)
				{
					if (CharMap[c1] != -1)
						continue;

					var c2 = matchCase ? c1 : c1.ToggleCase();
					CharMap[c1] = CharMap[c2] = NumChars++;
				}
			}
		}

		class SearchData
		{
			public readonly SearchData[] data;
			public bool done { get; private set; }
			public int length { get; private set; }
			public readonly FindData findData;
			readonly int maxChars;

			public SearchData(FindData findData, int maxChars)
			{
				data = new SearchData[maxChars];
				this.findData = findData;
				this.maxChars = maxChars;
			}

			public void Add(String str)
			{
				var node = this;
				foreach (var c in str)
				{
					var mappedIndex = findData.CharMap[c];
					if (node.data[mappedIndex] == null)
						node.data[mappedIndex] = new SearchData(findData, maxChars);
					node = node.data[mappedIndex];
				}
				node.done = true;
				node.length = str.Length;
			}

			public void Add(Char[] str)
			{
				var node = this;
				foreach (var c in str)
				{
					var mappedIndex = findData.CharMap[c];
					if (node.data[mappedIndex] == null)
						node.data[mappedIndex] = new SearchData(findData, maxChars);
					node = node.data[mappedIndex];
				}
				node.done = true;
				node.length = str.Length;
			}

			public void Add(Byte[] str)
			{
				var node = this;
				foreach (var c in str)
				{
					var mappedIndex = findData.CharMap[c];
					if (node.data[mappedIndex] == null)
						node.data[mappedIndex] = new SearchData(findData, maxChars);
					node = node.data[mappedIndex];
				}
				node.done = true;
				node.length = str.Length;
			}
		}

		readonly SearchData matchCaseSearcher;
		readonly SearchData ignoreCaseSearcher;
		public readonly int MaxLen = 0;

		public Searcher(List<String> strs, bool matchCase = false) : this(strs, strs.Select(a => matchCase).ToList()) { }
		public Searcher(List<Char[]> strs, bool matchCase = false) : this(strs, strs.Select(a => matchCase).ToList()) { }
		public Searcher(List<Byte[]> strs, bool matchCase = false) : this(strs, strs.Select(a => matchCase).ToList()) { }

		public Searcher(List<String> strs, List<bool> matchCase)
		{
			if (strs.Count != matchCase.Count)
				throw new Exception("Count mismatch.");

			var matchCaseFindData = new FindData((String)null, true);
			var ignoreCaseFindData = new FindData((String)null, false);
			for (var ctr = 0; ctr < strs.Count; ++ctr)
			{
				var node = matchCase[ctr] ? matchCaseFindData : ignoreCaseFindData;
				node.Add(strs[ctr]);
				MaxLen = Math.Max(MaxLen, strs[ctr].Length);
			}

			var maxChars = Math.Max(matchCaseFindData.NumChars, ignoreCaseFindData.NumChars);

			matchCaseSearcher = new SearchData(matchCaseFindData, maxChars);
			ignoreCaseSearcher = new SearchData(ignoreCaseFindData, maxChars);

			for (var ctr = 0; ctr < strs.Count; ++ctr)
			{
				var node = matchCase[ctr] ? matchCaseSearcher : ignoreCaseSearcher;
				node.Add(strs[ctr]);
			}
		}

		public Searcher(List<Char[]> strs, List<bool> matchCase)
		{
			if (strs.Count != matchCase.Count)
				throw new Exception("Count mismatch.");

			var matchCaseFindData = new FindData((Char[])null, true);
			var ignoreCaseFindData = new FindData((Char[])null, false);
			for (var ctr = 0; ctr < strs.Count; ++ctr)
			{
				var node = matchCase[ctr] ? matchCaseFindData : ignoreCaseFindData;
				node.Add(strs[ctr]);
				MaxLen = Math.Max(MaxLen, strs[ctr].Length);
			}

			var maxChars = Math.Max(matchCaseFindData.NumChars, ignoreCaseFindData.NumChars);

			matchCaseSearcher = new SearchData(matchCaseFindData, maxChars);
			ignoreCaseSearcher = new SearchData(ignoreCaseFindData, maxChars);

			for (var ctr = 0; ctr < strs.Count; ++ctr)
			{
				var node = matchCase[ctr] ? matchCaseSearcher : ignoreCaseSearcher;
				node.Add(strs[ctr]);
			}
		}

		public Searcher(List<Byte[]> strs, List<bool> matchCase)
		{
			if (strs.Count != matchCase.Count)
				throw new Exception("Count mismatch.");

			var matchCaseFindData = new FindData((Byte[])null, true);
			var ignoreCaseFindData = new FindData((Byte[])null, false);
			for (var ctr = 0; ctr < strs.Count; ++ctr)
			{
				var node = matchCase[ctr] ? matchCaseFindData : ignoreCaseFindData;
				node.Add(strs[ctr]);
				MaxLen = Math.Max(MaxLen, strs[ctr].Length);
			}

			var maxChars = Math.Max(matchCaseFindData.NumChars, ignoreCaseFindData.NumChars);

			matchCaseSearcher = new SearchData(matchCaseFindData, maxChars);
			ignoreCaseSearcher = new SearchData(ignoreCaseFindData, maxChars);

			for (var ctr = 0; ctr < strs.Count; ++ctr)
			{
				var node = matchCase[ctr] ? matchCaseSearcher : ignoreCaseSearcher;
				node.Add(strs[ctr]);
			}
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

		public List<Tuple<int, int>> Find(String input, int index, int length, bool firstOnly = false)
		{
			var result = new List<Tuple<int, int>>();
			length += index;
			var working = new List<SearchData>();
			for (var inputPos = index; inputPos < length; ++inputPos)
			{
				bool found = false;

				if (matchCaseSearcher.findData.CharMap[input[inputPos]] != -1)
				{
					working.Add(matchCaseSearcher);
					found = true;
				}

				if (ignoreCaseSearcher.findData.CharMap[input[inputPos]] != -1)
				{
					working.Add(ignoreCaseSearcher);
					found = true;
				}

				// Quick check: if the current char doesn't appear in the search at all, skip everything and go on
				if (!found)
				{
					working.Clear();
					continue;
				}


				var newWorking = new List<SearchData>();
				foreach (var worker in working)
				{
					var newWorker = worker.data[worker.findData.CharMap[input[inputPos]]];
					if (newWorker == null)
						continue;

					newWorking.Add(newWorker);

					if (newWorker.done)
					{
						result.Add(new Tuple<int, int>(inputPos - newWorker.length + 1, newWorker.length));
						if (firstOnly)
							return result;
						newWorking.Clear();
						break;
					}
				}
				working = newWorking;
			}
			return result;
		}

		public List<Tuple<int, int>> Find(Char[] input, int index, int length, bool firstOnly = false)
		{
			var result = new List<Tuple<int, int>>();
			length += index;
			var working = new List<SearchData>();
			for (var inputPos = index; inputPos < length; ++inputPos)
			{
				bool found = false;

				if (matchCaseSearcher.findData.CharMap[input[inputPos]] != -1)
				{
					working.Add(matchCaseSearcher);
					found = true;
				}

				if (ignoreCaseSearcher.findData.CharMap[input[inputPos]] != -1)
				{
					working.Add(ignoreCaseSearcher);
					found = true;
				}

				// Quick check: if the current char doesn't appear in the search at all, skip everything and go on
				if (!found)
				{
					working.Clear();
					continue;
				}


				var newWorking = new List<SearchData>();
				foreach (var worker in working)
				{
					var newWorker = worker.data[worker.findData.CharMap[input[inputPos]]];
					if (newWorker == null)
						continue;

					newWorking.Add(newWorker);

					if (newWorker.done)
					{
						result.Add(new Tuple<int, int>(inputPos - newWorker.length + 1, newWorker.length));
						if (firstOnly)
							return result;
						newWorking.Clear();
						break;
					}
				}
				working = newWorking;
			}
			return result;
		}

		public List<Tuple<int, int>> Find(Byte[] input, int index, int length, bool firstOnly = false)
		{
			var result = new List<Tuple<int, int>>();
			length += index;
			var working = new List<SearchData>();
			for (var inputPos = index; inputPos < length; ++inputPos)
			{
				bool found = false;

				if (matchCaseSearcher.findData.CharMap[input[inputPos]] != -1)
				{
					working.Add(matchCaseSearcher);
					found = true;
				}

				if (ignoreCaseSearcher.findData.CharMap[input[inputPos]] != -1)
				{
					working.Add(ignoreCaseSearcher);
					found = true;
				}

				// Quick check: if the current char doesn't appear in the search at all, skip everything and go on
				if (!found)
				{
					working.Clear();
					continue;
				}


				var newWorking = new List<SearchData>();
				foreach (var worker in working)
				{
					var newWorker = worker.data[worker.findData.CharMap[input[inputPos]]];
					if (newWorker == null)
						continue;

					newWorking.Add(newWorker);

					if (newWorker.done)
					{
						result.Add(new Tuple<int, int>(inputPos - newWorker.length + 1, newWorker.length));
						if (firstOnly)
							return result;
						newWorking.Clear();
						break;
					}
				}
				working = newWorking;
			}
			return result;
		}
	}
}
