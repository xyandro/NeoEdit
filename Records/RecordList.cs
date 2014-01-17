using System.Collections.Generic;

namespace NeoEdit.Records
{
	public abstract class RecordList : Record
	{
		protected RecordList(string uri, RecordList parent) : base(uri, parent) { }
		public virtual IEnumerable<Record> Records { get { return new List<Record>(); } }
	}
}
