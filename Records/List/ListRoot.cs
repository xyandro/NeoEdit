using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.GUI.Records.List
{
	public class ListRoot : ListRecord
	{
		const int NumLists = 5;

		static List<ListDir> lists;
		static ListRoot()
		{
			lists = Enumerable.Range(1, 5).Select(num => new ListDir(String.Format("List {0}", num))).ToList();
		}

		public ListRoot() : base("Lists") { }

		public ListDir this[int index] { get { return lists[index - 1]; } }

		public override IEnumerable<Record> Records { get { return lists; } }
	}
}
