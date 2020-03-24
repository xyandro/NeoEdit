using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NeoEdit.Common;

namespace NeoEdit.Editor.Searchers
{
	public class RegexesSearcher : ISearcher
	{
		public readonly List<Regex> regexes = new List<Regex>();
		readonly bool firstMatchOnly, regexGroups;

		public RegexesSearcher(List<string> texts, bool wholeWords = false, bool matchCase = false, bool entireSelection = false, bool firstMatchOnly = false, bool regexGroups = false)
		{
			foreach (var text in texts)
			{
				var str = $"(?:{text})";
				if (wholeWords)
					str = $"\\b{str}\\b";
				if (entireSelection)
					str = $"\\A{str}\\Z";
				var options = RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.Multiline;
				if (!matchCase)
					options |= RegexOptions.IgnoreCase;
				regexes.Add(new Regex(str, options));
			}

			this.firstMatchOnly = firstMatchOnly;
			this.regexGroups = regexGroups;
		}

		public List<Range> Find(string input, int addOffset = 0)
		{
			var result = new List<Range>();
			foreach (var regex in regexes)
			{
				var matches = firstMatchOnly ? new List<Match> { regex.Match(input) } : regex.Matches(input).Cast<Match>();
				foreach (var match in matches)
				{
					if (!match.Success)
						continue;

					if ((!regexGroups) || (match.Groups.Count == 1))
						result.Add(Range.FromIndex(match.Index + addOffset, match.Length));
					else
						result.AddRange(match.Groups.Cast<Group>().Skip(1).Where(group => group.Success).SelectMany(group => group.Captures.Cast<Capture>()).Select(capture => Range.FromIndex(capture.Index + addOffset, capture.Length)));
					if ((firstMatchOnly) && (result.Count != 0))
						break;
				}
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
