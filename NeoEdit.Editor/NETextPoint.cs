using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public class NETextPoint
	{
		IReadOnlyList<Range> ranges;
		IReadOnlyList<string> strs;
		NETextPoint parent;

		public NETextPoint(IReadOnlyList<Range> ranges, IReadOnlyList<string> strs, NETextPoint parent)
		{
			if (ranges.Count != strs.Count)
				throw new Exception("Invalid number of arguments");

			this.ranges = ranges;
			this.strs = strs;
			this.parent = parent;
		}

		public static string MoveTo(string text, NETextPoint start, NETextPoint end)
		{
			var ancestor = FindAncestor(start, end);
			while (start != ancestor)
			{
				text = start.Apply(text);
				start = start.parent;
			}
			var steps = new Stack<NETextPoint>();
			while (end != ancestor)
			{
				steps.Push(end);
				end = end.parent;
			}
			while (steps.Count > 0)
				text = steps.Pop().Apply(text);
			return text;
		}

		static NETextPoint FindAncestor(NETextPoint neTextPoint1, NETextPoint neTextPoint2)
		{
			var seen = new HashSet<NETextPoint>();
			while (true)
			{
				if (neTextPoint1 != null)
				{
					if (seen.Contains(neTextPoint1))
						return neTextPoint1;
					seen.Add(neTextPoint1);
					neTextPoint1 = neTextPoint1.parent;
				}
				else if (neTextPoint2 == null)
					throw new Exception("Ancestor not found");

				if (neTextPoint2 != null)
				{
					if (seen.Contains(neTextPoint2))
						return neTextPoint2;
					seen.Add(neTextPoint2);
					neTextPoint2 = neTextPoint2.parent;
				}
			}
		}

		public string Apply(string text)
		{
			int? checkPos = null;
			foreach (var range in ranges)
			{
				if (!checkPos.HasValue)
					checkPos = range.Start;
				if (range.Start < checkPos)
					throw new Exception("Replace data out of order");
				checkPos = range.End;
			}

			var nextRanges = new List<Range>();
			var nextStrs = new List<string>();

			var change = 0;
			for (var ctr = 0; ctr < ranges.Count; ++ctr)
			{
				var undoRange = new Range(ranges[ctr].Start + change, ranges[ctr].Start + strs[ctr].Length + change);
				nextRanges.Add(undoRange);
				nextStrs.Add(text.Substring(ranges[ctr].Start, ranges[ctr].Length));
				change = undoRange.Anchor - ranges[ctr].End;
			}

			var sb = new StringBuilder();
			var dataPos = 0;
			for (var listIndex = 0; listIndex <= strs.Count; ++listIndex)
			{
				var position = text.Length;
				var length = 0;
				if (listIndex < ranges.Count)
				{
					position = ranges[listIndex].Start;
					length = ranges[listIndex].Length;
				}

				sb.Append(text, dataPos, position - dataPos);
				dataPos = position;

				if (listIndex < strs.Count)
					sb.Append(strs[listIndex]);
				dataPos += length;
			}

			ranges = nextRanges;
			strs = nextStrs;
			return sb.ToString();
		}

		public override string ToString() => string.Join(", ", Enumerable.Range(0, ranges.Count).Select(index => $"{ranges[index]} = {strs[index]}"));
	}
}
