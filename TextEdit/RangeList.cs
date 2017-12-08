using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.TextEdit
{
	class RangeList : IEnumerable<Range>
	{
		List<Range> items = new List<Range>();

		public RangeList(List<Range> items, bool deOverlap = true)
		{
			if (deOverlap)
				items = DeOverlap(items);
			this.items = items;
		}

		public int Count => items.Count;

		public int IndexOf(Range range) => items.IndexOf(range);

		public Range this[int index] => items[index];

		enum DeOverlapStep
		{
			Sort,
			DeOverlap,
			Done,
		}

		static DeOverlapStep GetDeOverlapStep(List<Range> items)
		{
			var result = DeOverlapStep.Done;
			Range last = null;
			foreach (var range in items)
			{
				if (last != null)
				{
					if ((range.Start < last.Start) || ((range.Start == last.Start) && (range.End < last.End)))
						return DeOverlapStep.Sort;

					if ((range.Start < last.End) || ((range.Start == last.Start) && (range.End == last.End)))
						result = DeOverlapStep.DeOverlap;
				}

				last = range;
			}

			return result;
		}

		static List<Range> DoDeOverlap(List<Range> items)
		{
			var result = new List<Range>();

			using (var enumerator = items.GetEnumerator())
			{
				var last = default(Range);

				while (true)
				{
					var range = enumerator.MoveNext() ? enumerator.Current : null;

					if ((last != null) && ((range == null) || (last.Start != range.Start)))
					{
						if ((range == null) || (last.End <= range.Start))
							result.Add(last);
						else if (last.Cursor < last.Anchor)
							result.Add(new Range(last.Start, range.Start));
						else
							result.Add(new Range(range.Start, last.Start));
						last = null;
					}

					if (range == null)
						break;

					if ((last != null) && (range.End <= last.End))
						continue;

					last = range;
				}
			}

			return result;
		}

		static List<Range> DeOverlap(List<Range> items)
		{
			while (true)
			{
				switch (GetDeOverlapStep(items))
				{
					case DeOverlapStep.Sort: items = items.OrderBy(range => range.Start).ThenBy(range => range.End).ToList(); break;
					case DeOverlapStep.DeOverlap: return DoDeOverlap(items);
					case DeOverlapStep.Done: return items;
					default: throw new Exception("Invalid step");
				}
			}
		}

		static int[] GetTranslateNums(params RangeList[] rangeLists)
		{
			var nums = new int[rangeLists.Sum(rangeList => rangeList.Count * 2)];
			var numsStart = 0;
			foreach (var rangeList in rangeLists)
			{
				var size = Math.Max(65536, (rangeList.Count + 31) / 32);
				Helpers.PartitionedParallelForEach(rangeList.Count, size, (start, end) =>
				{
					var numPos = numsStart + start * 2;
					for (var r = start; r < end; ++r)
					{
						nums[numPos++] = rangeList[r].Start;
						nums[numPos++] = rangeList[r].End;
					}
				});
				numsStart += rangeList.Count * 2;
			}

			Array.Sort(nums);

			var outPos = -1;
			for (var inPos = 0; inPos < nums.Length; ++inPos)
			{
				if ((outPos != -1) && (nums[inPos] == nums[outPos]))
					continue;
				nums[++outPos] = nums[inPos];
			}

			Array.Resize(ref nums, outPos + 1);
			return nums;
		}

		public static Tuple<int[], int[]> GetTranslateMap(List<Range> replaceRanges, List<string> strs, params RangeList[] rangeLists)
		{
			var translateNums = GetTranslateNums(rangeLists);
			var translateResults = new int[translateNums.Length];
			var replaceRange = 0;
			var offset = 0;
			var current = 0;
			while (current < translateNums.Length)
			{
				int start = int.MaxValue, end = int.MaxValue, length = 0;
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

				translateResults[current] = value + offset;
				++current;
			}

			return Tuple.Create(translateNums, translateResults);
		}

		public List<Range> Translate(Tuple<int[], int[]> translateMap)
		{
			var result = Helpers.PartitionedParallelForEach<Range>(Count, Math.Max(65536, (Count + 31) / 32), (start, end, list) =>
			{
				var current = 0;
				for (var ctr = start; ctr < end; ++ctr)
				{
					current = Array.IndexOf(translateMap.Item1, this[ctr].Start, current);
					var startPos = current;
					current = Array.IndexOf(translateMap.Item1, this[ctr].End, current);
					if (this[ctr].Cursor < this[ctr].Anchor)
						list.Add(new Range(translateMap.Item2[startPos], translateMap.Item2[current]));
					else
						list.Add(new Range(translateMap.Item2[current], translateMap.Item2[startPos]));
				}
			});
			return result;
		}

		public int BinaryFindFirst(Predicate<Range> predicate)
		{
			int min = 0, max = Count;
			while (min < max)
			{
				int mid = (min + max) / 2;
				if (predicate(items[mid]))
					max = mid;
				else
					min = mid + 1;
			}
			return min == Count ? -1 : min;
		}

		public int BinaryFindLast(Predicate<Range> predicate)
		{
			int min = -1, max = Count - 1;
			while (min < max)
			{
				int mid = (min + max + 1) / 2;
				if (predicate(items[mid]))
					min = mid;
				else
					max = mid - 1;
			}
			return min;
		}

		public IEnumerator<Range> GetEnumerator() => items.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();
	}
}
