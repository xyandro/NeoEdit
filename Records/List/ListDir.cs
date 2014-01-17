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

		protected override IEnumerable<Record> InternalRecords
		{
			get
			{
				return items;
			}
		}
	}
}
