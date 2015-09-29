﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.TextEdit.RevRegEx
{
	class RevRegExDataRepeat : RevRegExData
	{
		public RevRegExData Value { get; private set; }
		public int Min { get; private set; }
		public int Max { get; private set; }

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
					results.Add(String.Concat(current.Take(pos).Select(num => possibilities[num])));

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

		public override string ToString() { return String.Format("{0}{{{1},{2}}}", Value, Min, Max); }
	}
}