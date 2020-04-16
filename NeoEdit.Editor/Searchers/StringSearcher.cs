using System;
using System.Collections.Generic;
using NeoEdit.Common;

namespace NeoEdit.Editor.Searchers
{
	public class StringSearcher : ISearcher
	{
		readonly string findStr1, findStr2;
		readonly bool wholeWords, skipSpace, entireSelection, firstMatchOnly;

		public StringSearcher(string findStr, bool wholeWords = false, bool matchCase = false, bool skipSpace = false, bool entireSelection = false, bool firstMatchOnly = false)
		{
			findStr1 = matchCase ? findStr : findStr.ToUpperInvariant();
			findStr2 = matchCase ? findStr : findStr.ToLowerInvariant();
			this.wholeWords = wholeWords;
			this.skipSpace = skipSpace;
			this.entireSelection = entireSelection;
			this.firstMatchOnly = firstMatchOnly;
		}

		int MatchLength(string input, int index)
		{
			var findIndex = 0;
			var inputIndex = index;
			while (findIndex < findStr1.Length)
			{
				if (inputIndex >= input.Length)
					return -1;

				if ((findIndex == 0) && (wholeWords) && (!Helpers.IsWordBoundary(input, inputIndex)))
					return -1;

				if ((skipSpace) && (char.IsWhiteSpace(findStr1[findIndex])) && (char.IsWhiteSpace(input[inputIndex])))
				{
					while ((findIndex < findStr1.Length) && (char.IsWhiteSpace(findStr1[findIndex])))
						++findIndex;
					while ((inputIndex < input.Length) && (char.IsWhiteSpace(input[inputIndex])))
						++inputIndex;
					continue;
				}

				if ((input[inputIndex] != findStr1[findIndex]) && (input[inputIndex] != findStr2[findIndex]))
				{
					if ((!skipSpace) || (findIndex == 0) || (!char.IsWhiteSpace(input[inputIndex])) || (!Helpers.IsWordBoundary(findStr1, findIndex)))
						return -1;

					while ((inputIndex < input.Length) && (char.IsWhiteSpace(input[inputIndex])))
						++inputIndex;

					continue;
				}

				++inputIndex;
				++findIndex;
			}

			if ((wholeWords) && (!Helpers.IsWordBoundary(input, inputIndex)))
				return -1;

			// Include trailing space
			if (skipSpace)
				while ((inputIndex < input.Length) && (char.IsWhiteSpace(input[inputIndex])))
					++inputIndex;

			return inputIndex - index;
		}

		public List<Range> Find(string input, int addOffset = 0)
		{
			var result = new List<Range>();
			if ((entireSelection) && (!skipSpace) && (input.Length != findStr1.Length))
				return result;

			var index = 0;
			while (index <= input.Length)
			{
				var matchLength = MatchLength(input, index);
				if (matchLength != -1)
				{
					if ((entireSelection) && (matchLength != input.Length))
						return result;
					result.Add(Range.FromIndex(index + addOffset, matchLength));
					if (firstMatchOnly)
						return result;
					index += Math.Max(1, matchLength);
				}
				else
					++index;

				if (entireSelection)
					break;
			}

			return result;
		}
	}
}
