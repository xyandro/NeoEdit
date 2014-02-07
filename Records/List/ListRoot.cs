using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Records.List
{
	public class ListRoot : RecordRoot
	{
		const int NumLists = 5;

		public static ListRoot Static { get; private set; }

		List<ListDir> lists;
		internal ListRoot(Record parent)
			: base("Lists", parent)
		{
			if (Static != null)
				throw new Exception("Can only create root nodes once.");
			Static = this;
			lists = Enumerable.Range(1, 5).Select(num => new ListDir(String.Format("List {0}", num), this)).ToList();
		}

		public ListDir this[int index] { get { return lists[index - 1]; } }

		protected override IEnumerable<Record> InternalRecords { get { return lists; } }
	}
}
