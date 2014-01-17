using System;

namespace NeoEdit.Records
{
	public abstract class RecordItem : Record
	{
		protected RecordItem(string uri, RecordList parent) : base(uri, parent) { }
		public virtual byte[] Read(Int64 position, int bytes) { throw new NotImplementedException(); }
	}
}
