using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Records.List
{
	public class ListRoot : RecordRoot
	{
		public ListRoot(RecordList parent) : base("Lists", parent) { }

		public override Record GetRecord(string uri)
		{
			var record = Records.FirstOrDefault(a => a.FullName.Equals(uri, StringComparison.OrdinalIgnoreCase));
			if (record != null)
				return record;
			return null;
		}

		protected override IEnumerable<Record> InternalRecords
		{
			get
			{
				yield return new ListDir("List 1", this);
				yield return new ListDir("List 2", this);
				yield return new ListDir("List 3", this);
				yield return new ListDir("List 4", this);
				yield return new ListDir("List 5", this);
			}
		}
	}
}
