using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.GUI.Records.List
{
	public class ListDir : ListRecord
	{
		List<Record> items = new List<Record>();
		public ListDir(string uri) : base(uri) { }

		public override Record Parent { get { return new ListRoot(); } }

		public void Add(Record record)
		{
			if (items.Any(a => a.FullName == record.FullName))
				return;
			items.Add(record);
		}

		public override IEnumerable<Record> Records
		{
			get
			{
				foreach (var item in items)
					yield return item;
			}
		}

		//public override void RemoveChild(Record record)
		//{
		//	items.Remove(record);
		//	Refresh();
		//}
	}
}
