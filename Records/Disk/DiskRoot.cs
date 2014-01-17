using System.Collections.Generic;
using System.IO;

namespace NeoEdit.Records.Disk
{
	public class DiskRoot : RecordRoot
	{
		public DiskRoot() : base("Disks") { }

		public override Record GetRecord(string uri)
		{
			if ((uri.Equals(FullName, System.StringComparison.OrdinalIgnoreCase)) || (File.Exists(uri)) || (Directory.Exists(uri)))
				return base.GetRecord(uri);
			return null;
		}

		public override IEnumerable<Record> Records
		{
			get
			{
				foreach (var drive in DriveInfo.GetDrives())
					yield return new DiskDir(drive.Name.Substring(0, drive.Name.Length - 1).ToUpper());
			}
		}
	}
}
