using System.Collections.Generic;
using System.Management;

namespace NeoEdit.Records.Network
{
	public class NetworkDir : RecordList
	{
		public NetworkDir(string uri, RecordList parent) : base(uri, parent) { }

		protected override IEnumerable<Record> InternalRecords
		{
			get
			{
				using (var shares = new ManagementClass(FullName + @"\root\cimv2", "Win32_Share", new ObjectGetOptions()))
					foreach (var share in shares.GetInstances())
						yield return new Disk.DiskDir(FullName + @"\" + share["Name"], this);
			}
		}
	}
}
