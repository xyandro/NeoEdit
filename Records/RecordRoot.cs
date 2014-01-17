using System;
using System.Linq;

namespace NeoEdit.Records
{
	public abstract class RecordRoot : RecordList
	{
		protected RecordRoot(string uri) : base(uri) { }
		public override RecordList Parent { get { return new RootRecordList(); } }

		public virtual Record GetRecord(string uri)
		{
			var parts = uri.Split('\\').ToList();
			for (var ctr1 = parts.Count; ctr1 >= 0; --ctr1)
				for (var ctr2 = ctr1 + 1; ctr2 < parts.Count; ++ctr2)
					parts[ctr2] = parts[ctr1] + @"\" + parts[ctr2];

			Record record = this;
			while (record != null)
			{
				if (uri.Equals(record.FullName, StringComparison.OrdinalIgnoreCase))
					return record;

				if ((parts.Count == 0) || (!(record is RecordList)))
					break;
				var part = parts[0];
				parts.RemoveAt(0);

				record = (record as RecordList).Records.SingleOrDefault(a => a.FullName.Equals(part, StringComparison.OrdinalIgnoreCase));
			}

			throw new Exception(String.Format("Invalid input: {0}", uri));
		}
	}
}
