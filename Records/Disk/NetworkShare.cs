using System.Collections.Generic;
using System.Management;

namespace NeoEdit.GUI.Records.Disk
{
	public class NetworkShare : DiskRecord
	{
		public NetworkShare(string uri) : base(uri) { }

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
