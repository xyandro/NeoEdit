using System.Collections.Generic;
using System.IO;

namespace NeoEdit.Records.Disk
{
	public class DiskRoot : IRecordRoot
	{
		public IRecord GetRecord(string uri)
		{
			if ((uri == FullName) || (File.Exists(uri)) || (Directory.Exists(uri)))
				return RecordListProvider.GetRecord(uri, this);
			return null;
		}

		public IRecordList Parent { get { return new RootRecordList(); } }
		public string Name { get { return Name; } }
		public string FullName { get { return "Disks"; } }
		public IEnumerable<IRecord> Records
		{
			get
			{
				foreach (var drive in DriveInfo.GetDrives())
					yield return new DiskDir(drive.Name.Substring(0, drive.Name.Length - 1).ToUpper());
			}
		}
	}
}
