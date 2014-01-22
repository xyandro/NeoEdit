using System;
using System.Collections.Generic;

namespace NeoEdit.Records.List
{
	public class ListDir : RecordList
	{
		List<Record> items = new List<Record>();
		public ListDir(string uri, RecordList parent) : base(uri, parent) { }

		public void Add(Record record)
		{
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
