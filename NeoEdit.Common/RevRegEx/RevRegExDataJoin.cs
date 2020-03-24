using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Common.RevRegEx
{
	class RevRegExDataJoin : RevRegExData
	{
		public ReadOnlyCollection<RevRegExData> List { get; }
		public static RevRegExData Create(IEnumerable<RevRegExData> list) => list.Count() == 1 ? list.First() : new RevRegExDataJoin(list);
		RevRegExDataJoin(IEnumerable<RevRegExData> list) { List = new ReadOnlyCollection<RevRegExData>(list.ToList()); }
		public override List<string> GetPossibilities() => new List<string>(List.SelectMany(item => item.GetPossibilities()));
		public override long Count() => List.Sum(item => item.Count());
		public override string ToString() => $"({string.Join("|", List.Select(item => item.ToString()))})";
	}
}
