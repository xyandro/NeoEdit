using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public class NETextPoint
	{
		IReadOnlyList<NERange> ranges;
		IReadOnlyList<string> strs;
		NETextPoint parent;

		public NETextPoint(string text, IReadOnlyList<NERange> ranges, IReadOnlyList<string> strs, NETextPoint parent)
		{
			if (ranges.Count != strs.Count)
				throw new Exception("Invalid number of arguments");

			(ranges, strs) = Simplify(text, ranges, strs);
			this.ranges = ranges;
			this.strs = strs;
			this.parent = parent;
		}

		static int BeginMatchLength(string str1, string str2)
		{
			var max = Math.Min(str1.Length, str2.Length);
			for (var ctr = 0; ctr < max; ++ctr)
				if (str1[ctr] != str2[ctr])
					return ctr;
			return max;
		}

		static int EndMatchLength(string str1, string str2)
		{
			var max = Math.Min(str1.Length, str2.Length);
			for (var ctr = 0; ctr < max; ++ctr)
				if (str1[str1.Length - ctr - 1] != str2[str2.Length - ctr - 1])
					return ctr;
			return max;
		}

		static (IReadOnlyList<NERange>, IReadOnlyList<string>) Simplify(string text, IReadOnlyList<NERange> ranges, IReadOnlyList<string> strs)
		{
			List<NERange> newRanges = new List<NERange>();
			List<string> newStrs = new List<string>();

			for (var ctr = 0; ctr < ranges.Count; ++ctr)
			{
				var range = ranges[ctr];
				var str = strs[ctr];

				var len = BeginMatchLength(str, text[range.Start..range.End]);
				if (len != 0)
				{
					range = NERange.FromIndex(range.Start + len, range.Length - len);
					str = str[len..];
				}

				len = EndMatchLength(str, text[range.Start..range.End]);
				if (len != 0)
				{
					range = NERange.FromIndex(range.Start, range.Length - len);
					str = str[0..(str.Length - len)];
				}

				if ((range.Length != 0) || (str.Length != 0))
				{
					newRanges.Add(range);
					newStrs.Add(str);
				}
			}
			return (newRanges, newStrs);
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

			var nextRanges = new List<NERange>();
			var nextStrs = new List<string>();

			var change = 0;
			for (var ctr = 0; ctr < ranges.Count; ++ctr)
			{
				var undoRange = new NERange(ranges[ctr].Start + strs[ctr].Length + change, ranges[ctr].Start + change);
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
