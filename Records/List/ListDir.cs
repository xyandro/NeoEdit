﻿using System.Collections.Generic;
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

		protected override IEnumerable<Record> InternalRecords
		{
			get
			{
				foreach (var item in items)
					yield return item;
			}
		}

		public override void RemoveChild(Record record)
		{
			items.Remove(record);
			Refresh();
		}
	}
}
