using System.Collections.Generic;

namespace NeoEdit.Program.Searchers
{
	public class StringSearcher
	{
		readonly string findStr1, findStr2;
		readonly bool wholeWords, entireSelection, firstMatchOnly;

		public StringSearcher(string findStr, bool wholeWords = false, bool matchCase = false, bool entireSelection = false, bool firstMatchOnly = false)
		{
			findStr1 = matchCase ? findStr : findStr.ToUpperInvariant();
			findStr2 = matchCase ? findStr : findStr.ToLowerInvariant();
			this.wholeWords = wholeWords;
			this.entireSelection = entireSelection;
			this.firstMatchOnly = firstMatchOnly;
		}

		public List<Range> Find(string input, int addOffset = 0)
		{
			var result = new List<Range>();
			if ((entireSelection) && (input.Length != findStr1.Length))
				return result;

			var endSearch = input.Length - findStr1.Length;
			for (var index = 0; index <= endSearch;)
			{
				var found = true;
				for (var strIndex = 0; strIndex < findStr1.Length; ++strIndex)
				{
					if ((input[index + strIndex] != findStr1[strIndex]) && (input[index + strIndex] != findStr2[strIndex]))
					{
						found = false;
						break;
					}

					if ((wholeWords) && (strIndex == 0) && (!Helpers.IsWordBoundary(input, index)))
					{
						found = false;
						break;
					}
				}

				if ((found) && ((!wholeWords) || (Helpers.IsWordBoundary(input, index + findStr1.Length))))
				{
					result.Add(Range.FromIndex(index + addOffset, findStr1.Length));
					if (firstMatchOnly)
						return result;
					index += findStr1.Length;
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
