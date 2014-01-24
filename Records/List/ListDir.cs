using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Records.List
{
	public class ListDir : Record
	{
		List<Record> items = new List<Record>();
		public ListDir(string uri, Record parent) : base(uri, parent) { }

		public void Add(Record record)
		{
			if (items.Any(a => a.FullName == record.FullName))
				return;
			items.Add(record);
			Refresh();
		}

		protected override IEnumerable<Tuple<string, Func<string, Record>>> InternalRecords
		{
			get
			{
				foreach (var item in items)
					yield return new Tuple<string, Func<string, Record>>(item.FullName, a => item);
			}
		}
	}
}
