using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Common
{
	public static class LCS
	{
		public enum MatchType { Match, Mismatch, Gap }

		static void CalculateDiff<Type>(List<Type> list1, List<Type> list2, out List<MatchType> result1, out List<MatchType> result2)
		{
			// Calculate matrix using lists in reverse order so matching stays at start of string
			var lenArray = new short[list1.Count + 1, list2.Count + 1];
			for (var list1Pos = 1; list1Pos <= list1.Count; ++list1Pos)
				for (var list2Pos = 1; list2Pos <= list2.Count; ++list2Pos)
					lenArray[list1Pos, list2Pos] = list1[list1.Count - list1Pos].Equals(list2[list2.Count - list2Pos]) ? (short)(lenArray[list1Pos - 1, list2Pos - 1] + 1) : Math.Max(lenArray[list1Pos - 1, list2Pos], lenArray[list1Pos, list2Pos - 1]);

			result1 = new List<MatchType>();
			result2 = new List<MatchType>();
			var pos1 = list1.Count;
			var pos2 = list2.Count;
			while ((pos1 != 0) || (pos2 != 0))
			{
				if ((pos1 != 0) && (pos2 != 0) && (list1[list1.Count - pos1].Equals(list2[list2.Count - pos2])))
				{
					result1.Add(MatchType.Match);
					result2.Add(MatchType.Match);
					--pos1;
					--pos2;
				}
				else if ((pos1 != 0) && ((pos2 == 0) || (lenArray[pos1 - 1, pos2] > lenArray[pos1, pos2 - 1])))
				{
					result1.Add(MatchType.Mismatch);
					result2.Add(MatchType.Gap);
					--pos1;
				}
				else if ((pos2 != 0) && ((pos1 == 0) || (lenArray[pos1, pos2 - 1] > lenArray[pos1 - 1, pos2])))
				{
					result1.Add(MatchType.Gap);
					result2.Add(MatchType.Mismatch);
					--pos2;
				}
				else
				{
					result1.Add(MatchType.Mismatch);
					result2.Add(MatchType.Mismatch);
					--pos1;
					--pos2;
				}
			}
		}

		public static void GetLCS<Type>(IEnumerable<Type> input1, IEnumerable<Type> input2, out List<MatchType> output1, out List<MatchType> output2)
		{
			output1 = new List<MatchType>();
			output2 = new List<MatchType>();
			const int BufSize = 1024;
			var enum1 = input1.GetEnumerator();
			var enum2 = input2.GetEnumerator();
			var list1 = new List<Type>();
			var list2 = new List<Type>();
			var list1Working = true;
			var list2Working = true;
			List<MatchType> result1, result2;
			while ((list1Working) || (list2Working))
			{
				while ((list1Working) && (list1.Count < BufSize))
					if (list1Working = enum1.MoveNext())
						list1.Add(enum1.Current);
				while ((list2Working) && (list2.Count < BufSize))
					if (list2Working = enum2.MoveNext())
						list2.Add(enum2.Current);

				CalculateDiff(list1, list2, out result1, out result2);

				var keep = result1.FindLastIndex(val => val == MatchType.Match) + 1;
				if ((keep == 0) || (!list1Working) || (!list2Working))
					keep = result1.Count;

				output1.AddRange(result1.Take(keep));
				output2.AddRange(result2.Take(keep));

				list1.RemoveRange(0, list1.Count - result1.Skip(keep).Count(val => val != MatchType.Gap));
				list2.RemoveRange(0, list2.Count - result2.Skip(keep).Count(val => val != MatchType.Gap));
			}
		}
	}
}
