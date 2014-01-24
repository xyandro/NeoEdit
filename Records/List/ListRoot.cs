using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Records.List
{
	public class ListRoot : RecordRoot
	{
		public ListRoot(Record parent) : base("Lists", parent) { }

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
				for (var ctr = 1; ctr <= 5; ctr++)
					yield return new ListDir(String.Format("List {0}", ctr), this);
			}
		}
	}
}
