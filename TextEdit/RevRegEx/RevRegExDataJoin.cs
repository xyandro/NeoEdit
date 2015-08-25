using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NeoEdit.TextEdit.RevRegEx
{
	class RevRegExDataJoin : RevRegExData
	{
		public ReadOnlyCollection<RevRegExData> List { get; private set; }
		public static RevRegExData Create(IEnumerable<RevRegExData> list) { return list.Count() == 1 ? list.First() : new RevRegExDataJoin(list); }
		RevRegExDataJoin(IEnumerable<RevRegExData> list) { List = new ReadOnlyCollection<RevRegExData>(list.ToList()); }
		public override List<string> GetPossibilities() { return new List<string>(List.SelectMany(item => item.GetPossibilities())); }
		public override long Count() { return List.Sum(item => item.Count()); }
		public override string ToString() { return "(" + String.Join("|", List.Select(item => item.ToString())) + ")"; }
	}
}
