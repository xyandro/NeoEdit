using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit
{
	public static class LCS
	{
		public class LCSResult
		{
			public MatchType Match0 { get; }
			public MatchType Match1 { get; }

			public LCSResult(MatchType match0, MatchType match1)
			{
				Match0 = match0;
				Match1 = match1;
			}

			public MatchType this[int index] => index == 0 ? Match0 : Match1;
			public override string ToString() => $"{Match0}/{Match1}";
			public bool IsMatch => Match0 == MatchType.Match;
		}

		public enum MatchType { Match, Mismatch, Gap }

		static List<LCSResult> CalculateDiff<T>(List<T> list0, List<T> list1, Func<T, T, bool> keepTogether = null)
		{
			const int ValueBase = 0x00020000;
			const int CountBase = 0x00000004;
			const int Type = 0x00000003;
			const int Gap0 = 2;
			const int Gap1 = 1; // Gap0 > Gap1 so LCS will prefer left
			const int Match = 3;

			var lenArray = new int[list0.Count + 1, list1.Count + 1];
			for (var list0Pos = 1; list0Pos <= list0.Count; ++list0Pos)
				lenArray[list0Pos, 0] = Gap1;
			for (var list1Pos = 1; list1Pos <= list1.Count; ++list1Pos)
				lenArray[0, list1Pos] = Gap0;
			lenArray[0, 0] = Match;
			for (var list0Pos = 1; list0Pos <= list0.Count; ++list0Pos)
				for (var list1Pos = 1; list1Pos <= list1.Count; ++list1Pos)
				{
					var gap0Val = lenArray[list0Pos, list1Pos - 1] & ~Type | Gap0;
					var gap1Val = lenArray[list0Pos - 1, list1Pos] & ~Type | Gap1;
					var max = Math.Max(gap0Val, gap1Val);

					var item0 = list0[list0Pos - 1];
					var item1 = list1[list1Pos - 1];
					if (item0.Equals(item1))
					{
						var matchVal = (lenArray[list0Pos - 1, list1Pos - 1] & ~Type | Match) + ValueBase;
						if ((list0Pos > 1) && ((lenArray[list0Pos - 1, list1Pos - 1] & Type) == Match))
							if (keepTogether?.Invoke(list0[list0Pos - 2], item0) == true)
								matchVal += CountBase;
						max = Math.Max(max, matchVal);
					}

					lenArray[list0Pos, list1Pos] = max;
				}

			var result = new List<LCSResult>();
			var pos0 = list0.Count;
			var pos1 = list1.Count;
			while ((pos0 != 0) || (pos1 != 0))
			{
				switch (lenArray[pos0, pos1] & Type)
				{
					case Match: result.Add(new LCSResult(MatchType.Match, MatchType.Match)); break;
					case Gap0: result.Add(new LCSResult(MatchType.Gap, MatchType.Mismatch)); break;
					case Gap1: result.Add(new LCSResult(MatchType.Mismatch, MatchType.Gap)); break;
				}

				if (result[result.Count - 1].Match0 != MatchType.Gap)
					--pos0;
				if (result[result.Count - 1].Match1 != MatchType.Gap)
					--pos1;
			}
			result.Reverse();

			var gap1Pos = 0;
			while (true)
			{
				gap1Pos = result.FindIndex(gap1Pos, r => r.Match1 == MatchType.Gap);
				if (gap1Pos == -1)
					break;
				var gap0Pos = result.FindIndex(gap1Pos, r => r.Match1 != MatchType.Gap);
				if ((gap0Pos == -1) || (gap0Pos >= result.Count) || (result[gap0Pos].Match0 != MatchType.Gap))
				{
					++gap1Pos;
					continue;
				}
				var final = result.FindIndex(gap0Pos, r => r.Match0 != MatchType.Gap);
				if (final == -1)
					final = result.Count;
				var mismatchCount = Math.Min(final - gap0Pos, gap0Pos - gap1Pos);
				Enumerable.Range(gap1Pos, mismatchCount).ForEach(index => result[index] = new LCSResult(MatchType.Mismatch, MatchType.Mismatch));
				result.RemoveRange(gap0Pos, mismatchCount);
				gap1Pos = final - mismatchCount;
			}
			return result;
		}

		public static List<LCSResult> GetLCS<T>(IEnumerable<T> input0, IEnumerable<T> input1, Func<T, T, bool> keepTogether = null)
		{
			const int BufSize = 2048;
			const int BreakMatchCount = 10;

			var result = new List<LCSResult>();
			var enum0 = input0.GetEnumerator();
			var enum1 = input1.GetEnumerator();
			var list0 = new List<T>();
			var list1 = new List<T>();
			var list0Working = true;
			var list1Working = true;
			while ((list0Working) || (list1Working))
			{
				while ((list0Working) && (list0.Count < BufSize))
					if (list0Working = enum0.MoveNext())
						list0.Add(enum0.Current);
				while ((list1Working) && (list1.Count < BufSize))
					if (list1Working = enum1.MoveNext())
						list1.Add(enum1.Current);

				var diffResult = CalculateDiff(list0, list1, keepTogether);

				var keep = diffResult.Count;
				if ((list0Working) || ((list1Working)))
				{
					var lastNonMatch = keep;
					while (keep > 0)
					{
						--keep;
						if (diffResult[keep][0] != MatchType.Match)
						{
							lastNonMatch = keep;
							continue;
						}

						if (lastNonMatch - keep >= BreakMatchCount)
						{
							keep = lastNonMatch;
							break;
						}
					}
					if (keep == 0)
						keep = diffResult.Count;
				}

				result.AddRange(diffResult.Take(keep));

				list0.RemoveRange(0, diffResult.Take(keep).Count(val => val[0] != MatchType.Gap));
				list1.RemoveRange(0, diffResult.Take(keep).Count(val => val[1] != MatchType.Gap));
			}

			return result;
		}
	}
}
