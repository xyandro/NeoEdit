using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NeoEdit.Program.Searchers
{
	public class RegexSearcher : ISearcher
	{
		public readonly Regex regex;
		readonly bool firstMatchOnly, regexGroups;

		public RegexSearcher(string text, bool wholeWords = false, bool matchCase = false, bool entireSelection = false, bool firstMatchOnly = false, bool regexGroups = false)
		{
			text = $"(?:{text})";
			if (wholeWords)
				text = $"\\b{text}\\b";
			if (entireSelection)
				text = $"\\A{text}\\Z";
			var options = RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.Multiline;
			if (!matchCase)
				options |= RegexOptions.IgnoreCase;
			regex = new Regex(text, options);

			this.firstMatchOnly = firstMatchOnly;
			this.regexGroups = regexGroups;
		}

		public List<Range> Find(string input, int addOffset = 0)
		{
			var result = new List<Range>();
			var matches = regex.Matches(input).Cast<Match>();
			foreach (var match in matches)
			{
				if ((!regexGroups) || (match.Groups.Count == 1))
					result.Add(Range.FromIndex(match.Index + addOffset, match.Length));
				else
					result.AddRange(match.Groups.Cast<Group>().Skip(1).Where(group => group.Success).SelectMany(group => group.Captures.Cast<Capture>()).Select(capture => Range.FromIndex(capture.Index + addOffset, capture.Length)));
				if ((firstMatchOnly) && (result.Count != 0))
					break;
			}

			return result;
		}
	}
}
