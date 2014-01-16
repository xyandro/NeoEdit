using System;
using System.Collections.Generic;
using System.IO;

namespace NeoEdit.Records.Disk
{
	public class DiskRoot : IRecordList
	{
		static Func<String, IRecordList> Provider { get { return name => (name == "DiskRoot") ? new DiskRoot() : null; } }

		public IRecordList Parent { get { return new RootRecordList(); } }
		public string Name { get { return "DiskRoot"; } }
		public string FullName { get { return "DiskRoot"; } }
		public IEnumerable<IRecord> Records
		{
			get
			{
				foreach (var drive in DriveInfo.GetDrives())
					yield return new DiskDir(drive.Name);
			}
		}

		public override string ToString()
		{
			return "DiskRoot";
		}
	}
}
