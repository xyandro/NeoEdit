using System;

namespace NeoEdit.Records.Lists
{
	public abstract class ListRecord : Record
	{
		public ListRecord(string uri) : base(uri) { }

		public override Type GetRootType() { return typeof(ListRecord); }
	}
}
