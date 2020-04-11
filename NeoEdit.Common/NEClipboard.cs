using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Common
{
	public class NEClipboard : IEnumerable<IReadOnlyList<string>>
	{
		public static NEClipboard System { get; set; }
		public static NEClipboard Current { get; set; }

		List<IReadOnlyList<string>> stringLists = new List<IReadOnlyList<string>>();
		public bool? IsCut { get; set; } = null;

		public void Add(IReadOnlyList<string> items) => stringLists.Add(items);
		public int Count => stringLists.Count;
		public int ChildCount => stringLists.Sum(list => list.Count);

		public string String => string.Join("\r\n", Strings);
		public IReadOnlyList<string> Strings => stringLists.SelectMany().ToList();

		public IEnumerator<IReadOnlyList<string>> GetEnumerator() => stringLists.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
