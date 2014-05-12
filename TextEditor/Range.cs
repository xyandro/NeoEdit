using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.TextEditor
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
		}
		public static Range FromIndex(int index, int length) { return new Range(index, index + length); }

		public int Cursor { get; private set; }
		public int Highlight { get; private set; }
		public int Start { get; private set; }
		public int End { get; private set; }
		public int Length { get; private set; }

		public bool HasSelection()
		{
			return Cursor != Highlight;
		}

		public override string ToString()
		{
			return String.Format("({0:0000000000})->({1:0000000000})", Start, End);
		}

		public bool Equals(Range range)
		{
			return (Start == range.Start) && (End == range.End);
		}

		public override bool Equals(object obj)
		{
			if (obj is Range)
				return Equals(obj as Range);
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}

	static class RangeExtensions
	{
		public static void DeOverlap(this RangeList ranges)
		{
			var newRanges = ranges.Distinct().OrderBy(range => range.Start).ToList();
			for (var ctr = 0; ctr < newRanges.Count - 1; ++ctr)
				newRanges[ctr] = new Range(Math.Min(newRanges[ctr].Cursor, newRanges[ctr + 1].Start), Math.Min(newRanges[ctr].Highlight, newRanges[ctr + 1].Start));
			ranges.Replace(newRanges);
		}

		public static List<int> GetTranslateNums(params RangeList[] ranges)
		{
			return ranges.SelectMany(list => list).SelectMany(range => new int[] { range.Start, range.End }).Distinct().OrderBy(num => num).ToList();
		}

		public static Dictionary<int, int> GetTranslateMap(List<int> translateNums, RangeList replaceRanges, List<string> strs)
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

		public static void Translate(this RangeList ranges, Dictionary<int, int> translateMap)
		{
			ranges.Replace(ranges.Select(range => new Range(translateMap[range.Cursor], translateMap[range.Highlight])).ToList());
		}
	}

}
