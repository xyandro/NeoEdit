using System.Collections.Generic;
using System.Management;

namespace NeoEdit.Records.Network
{
	public class NetworkDir : NetworkRecord
	{
		public NetworkDir(string uri) : base(uri) { }

		public override Record Parent { get { return new NetworkRoot(); } }

		public override IEnumerable<Record> Records
		{
			get
			{
				using (var shares = new ManagementClass(FullName + @"\root\cimv2", "Win32_Share", new ObjectGetOptions()))
					foreach (var share in shares.GetInstances())
						yield return new Disk.DiskDir(FullName + @"\" + share["Name"]);
			}
		}
	}
}
