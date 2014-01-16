using System;
using System.Collections.Generic;
using System.IO;

namespace NeoEdit.Records.Disk
{
	public class DiskRoot : IRecordList
	{
		static Func<String, IRecordList> Provider { get { return name => name.Equals(RootName, StringComparison.OrdinalIgnoreCase) ? new DiskRoot() : null; } }
		public static string RootName { get { return "Disks"; } }

		public IRecordList Parent { get { return new RootRecordList(); } }
		public string Name { get { return RootName; } }
		public string FullName { get { return RootName; } }
		public IEnumerable<IRecord> Records
		{
			get
			{
				foreach (var drive in DriveInfo.GetDrives())
					yield return new DiskDir(drive.Name.ToUpper());
			}
		}
	}
}
