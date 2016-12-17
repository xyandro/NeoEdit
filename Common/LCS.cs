using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Common
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

		static List<LCSResult> CalculateDiff<T>(List<T> list0, List<T> list1)
		{
			// Calculate matrix using lists in reverse order so matching stays at start of string
			var lenArray = new short[list0.Count + 1, list1.Count + 1];
			for (var list0Pos = 1; list0Pos <= list0.Count; ++list0Pos)
				for (var list1Pos = 1; list1Pos <= list1.Count; ++list1Pos)
					lenArray[list0Pos, list1Pos] = list0[list0.Count - list0Pos].Equals(list1[list1.Count - list1Pos]) ? (short)(lenArray[list0Pos - 1, list1Pos - 1] + 1) : Math.Max(lenArray[list0Pos - 1, list1Pos], lenArray[list0Pos, list1Pos - 1]);

			var result = new List<LCSResult>();
			var pos0 = list0.Count;
			var pos1 = list1.Count;
			while ((pos0 != 0) || (pos1 != 0))
			{
				if ((pos0 != 0) && (pos1 != 0) && (list0[list0.Count - pos0].Equals(list1[list1.Count - pos1])))
					result.Add(new LCSResult(MatchType.Match, MatchType.Match));
				else if ((pos0 != 0) && ((pos1 == 0) || (lenArray[pos0 - 1, pos1] > lenArray[pos0, pos1 - 1])))
					result.Add(new LCSResult(MatchType.Mismatch, MatchType.Gap));
				else if ((pos1 != 0) && ((pos0 == 0) || (lenArray[pos0, pos1 - 1] > lenArray[pos0 - 1, pos1])))
					result.Add(new LCSResult(MatchType.Gap, MatchType.Mismatch));
				else
					result.Add(new LCSResult(MatchType.Mismatch, MatchType.Mismatch));

				if (result[result.Count - 1].Match0 != MatchType.Gap)
					--pos0;
				if (result[result.Count - 1].Match1 != MatchType.Gap)
					--pos1;
			}
			return result;
		}

		public static List<LCSResult> GetLCS<T>(IEnumerable<T> input0, IEnumerable<T> input1)
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

				var diffResult = CalculateDiff(list0, list1);

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

				list0.RemoveRange(0, diffResult.Take(keep).Count(val => val.Match0 != MatchType.Gap));
				list1.RemoveRange(0, diffResult.Take(keep).Count(val => val.Match1 != MatchType.Gap));
			}

			return result;
		}
	}
}
