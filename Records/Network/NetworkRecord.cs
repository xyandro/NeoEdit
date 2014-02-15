using System;

namespace NeoEdit.Records.Network
{
	public class NetworkRecord : Record
	{
		public NetworkRecord(string uri) : base(uri) { }

		public override Type GetRootType() { return typeof(NetworkRecord); }
	}
}
