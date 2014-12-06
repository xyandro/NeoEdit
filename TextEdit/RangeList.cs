using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEdit
{
	class RangeList : List<Range>
	{
		readonly RunOnceTimer timer;

		public RunOnceTimer Timer { get { return timer; } }
		public bool Changed { get { return timer.Started; } }

		public RangeList(Action callback)
		{
			timer = new RunOnceTimer(() =>
			{
				callback();
				timer.Stop();
			});
		}

		void Signal()
		{
			timer.Start();
		}

		public new Range this[int index]
		{
			get { return base[index]; }
			set
			{
				base[index] = value;
				Signal();
			}
		}

		public new void AddRange(IEnumerable<Range> items)
		{
			if (!items.Any())
				return;
			base.AddRange(items);
			Signal();
		}

		public new void Insert(int index, Range item)
		{
			base.Insert(index, item);
			Signal();
		}

		public new bool Remove(Range item)
		{
			Signal();
			return base.Remove(item);
		}

		public new void RemoveAt(int index)
		{
			base.RemoveAt(index);
			Signal();
		}

		public new void RemoveRange(int index, int count)
		{
			base.RemoveRange(index, count);
			Signal();
		}

		public void Replace(Range item)
		{
			Clear();
			Add(item);
		}

		public void Replace(IEnumerable<Range> items)
		{
			Clear();
			AddRange(items);
		}

		public new void Clear()
		{
			if (Count == 0)
				return;
			Signal();
			base.Clear();
		}

		public new void Add(Range item)
		{
			base.Add(item);
			Signal();
		}

		enum DeOverlapStep
		{
			Sort,
			DeOverlap,
			Done,
		}

		DeOverlapStep GetDeOverlapStep()
		{
			var result = DeOverlapStep.Done;
			Range last = null;
			foreach (var range in this)
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

		void DoDeOverlap()
		{
			var result = new List<Range>();

			using (var enumerator = this.GetEnumerator())
			{
				var last = default(Range);

				while (true)
				{
					var range = enumerator.MoveNext() ? enumerator.Current : null;

					if ((last != null) && ((range == null) || (last.Start != range.Start)))
					{
						if ((range == null) || (last.End <= range.Start))
							result.Add(last);
						else if (last.Cursor < last.Highlight)
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

			Replace(result);
		}

		public void DeOverlap()
		{
			while (true)
			{
				switch (GetDeOverlapStep())
				{
					case DeOverlapStep.Sort: Replace(this.OrderBy(range => range.Start).ThenBy(range => range.End).ToList()); break;
					case DeOverlapStep.DeOverlap: DoDeOverlap(); return;
					case DeOverlapStep.Done: return;
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
				rangeList.PartitionedParallelForEach(size, (start, end) =>
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

		public void Translate(Tuple<int[], int[]> translateMap)
		{
			var result = this.PartitionedParallelForEach<Range, Range>(Math.Max(65536, (Count + 31) / 32), (start, end, list) =>
			{
				var current = 0;
				for (var ctr = start; ctr < end; ++ctr)
				{
					current = Array.IndexOf(translateMap.Item1, this[ctr].Start, current);
					var startPos = current;
					current = Array.IndexOf(translateMap.Item1, this[ctr].End, current);
					if (this[ctr].Cursor < this[ctr].Highlight)
						list.Add(new Range(translateMap.Item2[startPos], translateMap.Item2[current]));
					else
						list.Add(new Range(translateMap.Item2[current], translateMap.Item2[startPos]));
				}
			});
			Replace(result);
		}

		public int BinaryFindFirst(Predicate<Range> predicate)
		{
			int min = 0, max = Count;
			while (min < max)
			{
				int mid = (min + max) / 2;
				if (predicate(base[mid]))
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
				if (predicate(base[mid]))
					min = mid;
				else
					max = mid - 1;
			}
			return min;
		}
	}
}
