using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NeoEdit.Program;

namespace NeoEdit.Program.RevRegEx
{
	class RevRegExDataList : RevRegExData
	{
		public ReadOnlyCollection<RevRegExData> List { get; }
		public static RevRegExData Create(IEnumerable<RevRegExData> list) => list.Count() == 1 ? list.First() : new RevRegExDataList(list);
		RevRegExDataList(IEnumerable<RevRegExData> list) { List = new ReadOnlyCollection<RevRegExData>(list.ToList()); }

		public override List<string> GetPossibilities()
		{
			var results = new List<string>();
			var possibilities = List.Select(item => item.GetPossibilities()).ToList();
			if (!possibilities.Any())
			{
				results.Add("");
				return results;
			}
			var current = possibilities.Select(item => -1).ToList();
			var pos = 0;
			while (true)
			{
				++current[pos];
				if (current[pos] >= possibilities[pos].Count)
				{
					--pos;
					if (pos < 0)
						break;
					continue;
				}

				++pos;
				if (pos < current.Count)
					current[pos] = -1;
				else
				{
					results.Add(string.Concat(current.Select((num, index) => possibilities[index][num])));
					--pos;
				}
			}
			return results;
		}
		public override long Count()
		{
			var counts = List.Select(item => item.Count()).ToList();
			long result = 1;
			foreach (var count in counts)
				result *= count;
			return result;
		}

		public override string ToString() => $"({string.Join("", List.Select(item => item.ToString()))})";
	}
}
