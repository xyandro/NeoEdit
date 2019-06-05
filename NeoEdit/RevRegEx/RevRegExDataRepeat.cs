using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Parsing;

namespace NeoEdit.RevRegEx
{
	class RevRegExDataRepeat : RevRegExData
	{
		public RevRegExData Value { get; }
		public int Min { get; }
		public int Max { get; }

		public RevRegExDataRepeat(RevRegExData value, int min, int max) { Value = value; Min = min; Max = max; }
		public override List<string> GetPossibilities()
		{
			var results = new List<string>();
			if (Min <= 0)
				results.Add("");
			var possibilities = Value.GetPossibilities();
			var current = Enumerable.Range(0, Max).Select(item => -1).ToList();
			var pos = 0;
			while (true)
			{
				++current[pos];
				if (current[pos] >= possibilities.Count)
				{
					--pos;
					if (pos < 0)
						break;
					continue;
				}

				++pos;
				if (pos < current.Count)
					current[pos] = -1;

				if (pos >= Min)
					results.Add(string.Concat(current.Take(pos).Select(num => possibilities[num])));

				if (pos >= current.Count)
					--pos;
			}
			return results;
		}

		long IntPow(long count, long ctr)
		{
			long value = 1;
			for (var powCtr = 1; powCtr < ctr; ++powCtr)
				value *= count;
			return value;
		}

		public override long Count()
		{
			var count = Value.Count();

			long value = 1;
			long result = 0;
			// SUM(Min <= e <= Max, count^e)
			for (var ctr = 0; ctr <= Max; ++ctr)
			{
				if (ctr >= Min)
					result += value;
				value *= count;
			}

			return result;
		}

		public override string ToString() => $"{Value}{{{Min},{Max}}}";
	}
}
