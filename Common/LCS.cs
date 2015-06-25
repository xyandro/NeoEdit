using System.Collections.Generic;

namespace NeoEdit.Common
{
	public static class LCS
	{
		public enum MatchType { None, List1Gap, List2Gap, Match, Mismatch, Gap }

		static MatchType[,] GetDirArray<Type>(List<Type> list1, List<Type> list2)
		{
			var dirArray = new MatchType[list1.Count + 1, list2.Count + 1];
			var lenArray = new int[list1.Count + 1, list2.Count + 1];

			for (var ctr = 0; ctr < list2.Count; ++ctr)
			{
				dirArray[list1.Count, ctr] = MatchType.List1Gap;
				lenArray[list1.Count, ctr] = ctr;
			}

			for (var ctr = 0; ctr < list1.Count; ++ctr)
			{
				dirArray[ctr, list2.Count] = MatchType.List2Gap;
				lenArray[ctr, list2.Count] = ctr;
			}

			for (var list1Pos = list1.Count - 1; list1Pos >= 0; --list1Pos)
				for (var list2Pos = list2.Count - 1; list2Pos >= 0; --list2Pos)
				{
					var matches = list1[list1Pos].Equals(list2[list2Pos]);
					var match = lenArray[list1Pos + 1, list2Pos + 1] + (matches ? -5 : 1);
					var list1Gap = lenArray[list1Pos, list2Pos + 1] + 1;
					var list2Gap = lenArray[list1Pos + 1, list2Pos] + 1;

					if ((match <= list1Gap) && (match <= list2Gap))
					{
						dirArray[list1Pos, list2Pos] = matches ? MatchType.Match : MatchType.Mismatch;
						lenArray[list1Pos, list2Pos] = match;
					}
					else if (list1Gap <= list2Gap)
					{
						dirArray[list1Pos, list2Pos] = MatchType.List1Gap;
						lenArray[list1Pos, list2Pos] = list1Gap;
					}
					else
					{
						dirArray[list1Pos, list2Pos] = MatchType.List2Gap;
						lenArray[list1Pos, list2Pos] = list2Gap;
					}
				}

			return dirArray;
		}

		static void GetLCS(MatchType[,] dirArray, out List<MatchType> result1, out List<MatchType> result2)
		{
			int list1Pos = 0, list2Pos = 0;
			result1 = new List<MatchType>();
			result2 = new List<MatchType>();
			var done = false;
			while (!done)
			{
				var dir = dirArray[list1Pos, list2Pos];
				switch (dir)
				{
					case MatchType.None:
						done = true;
						break;
					case MatchType.Match:
					case MatchType.Mismatch:
						result1.Add(dir);
						result2.Add(dir);
						++list1Pos;
						++list2Pos;
						break;
					case MatchType.List1Gap:
						result1.Add(MatchType.Gap);
						result2.Add(MatchType.Mismatch);
						++list2Pos;
						break;
					case MatchType.List2Gap:
						result1.Add(MatchType.Mismatch);
						result2.Add(MatchType.Gap);
						++list1Pos;
						break;
				}
			}
		}

		public static void GetLCS<Type>(List<Type> input1, List<Type> input2, out List<MatchType> output1, out List<MatchType> output2)
		{
			var dirArray = GetDirArray(input1, input2);
			GetLCS(dirArray, out output1, out output2);
		}
	}
}
