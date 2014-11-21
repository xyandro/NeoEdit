using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NeoEdit.TextEdit
{
	class Range
	{
		public Range() : this(0) { }
		public Range(int pos) : this(pos, pos) { }
		public Range(int cursor, int highlight)
		{
			Cursor = cursor;
			Highlight = highlight;
			Start = Math.Min(Cursor, Highlight);
			End = Math.Max(Cursor, Highlight);
			Length = Math.Abs(Cursor - Highlight);
			HasSelection = Length != 0;
		}
		public static Range FromIndex(int index, int length) { return new Range(index + length, index); }

		public int Cursor { get; private set; }
		public int Highlight { get; private set; }
		public int Start { get; private set; }
		public int End { get; private set; }
		public int Length { get; private set; }
		public bool HasSelection { get; private set; }

		public override string ToString()
		{
			return String.Format("({0:0000000000})->({1:0000000000})", Start, End);
		}
	}

	static class RangeExtensions
	{
		public static void DeOverlap(this ObservableCollection<Range> ranges)
		{
			if (ranges.Count == 0)
				return;

			var end = new Dictionary<int, int>();
			var cursorFirst = new Dictionary<int, bool>();
			foreach (var range in ranges)
			{
				if ((end.ContainsKey(range.Start)) && (range.End <= end[range.Start]))
					continue;

				end[range.Start] = range.End;
				cursorFirst[range.Start] = range.Cursor < range.Highlight;
			}
			var start = end.Keys.ToList();
			start.Sort();
			start.Add(end[start[start.Count - 1]]);

			ranges.Clear();
			for (var ctr = 0; ctr < start.Count - 1; ++ctr)
			{
				var rangeStart = start[ctr];
				var rangeEnd = Math.Min(end[rangeStart], start[ctr + 1]);
				if (cursorFirst[rangeStart])
					ranges.Add(new Range(rangeStart, rangeEnd));
				else
					ranges.Add(new Range(rangeEnd, rangeStart));
			}
		}

		public static List<int> GetTranslateNums(params IEnumerable<Range>[] ranges)
		{
			return ranges.SelectMany(list => list).SelectMany(range => new int[] { range.Start, range.End }).Distinct().OrderBy(num => num).ToList();
		}

		public static Dictionary<int, int> GetTranslateMap(List<int> translateNums, IList<Range> replaceRanges, List<string> strs)
		{
			var translateMap = new Dictionary<int, int>();
			var replaceRange = 0;
			var offset = 0;
			var current = 0;
			while (current < translateNums.Count)
			{
				int start = Int32.MaxValue, end = Int32.MaxValue, length = 0;
				if (replaceRange < replaceRanges.Count)
				{
					start = replaceRanges[replaceRange].Start;
					end = replaceRanges[replaceRange].End;
					length = strs[replaceRange].Length;
				}

				if (translateNums[current] >= end)
				{
					offset += start - end + length;
					++replaceRange;
					continue;
				}

				var value = translateNums[current];
				if ((value > start) && (value < end))
					value = start + length;

				translateMap[translateNums[current]] = value + offset;
				++current;
			}

			return translateMap;
		}

		public static void Translate(this ObservableCollection<Range> ranges, Dictionary<int, int> translateMap)
		{
			ranges.Replace(ranges.Select(range => new Range(translateMap[range.Cursor], translateMap[range.Highlight])).ToList());
		}
	}

}
