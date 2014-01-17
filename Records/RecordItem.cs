using System;

namespace NeoEdit.Records
{
	public abstract class RecordItem : Record
	{
		protected RecordItem(string uri) : base(uri) { }
		public virtual Int64 Size { get; protected set; }
		public virtual byte[] Read(Int64 position, int bytes) { throw new NotImplementedException(); }
	}
}
