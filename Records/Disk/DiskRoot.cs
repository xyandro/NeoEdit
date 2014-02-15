using System.Collections.Generic;
using System.IO;

namespace NeoEdit.Records.Disk
{
	public class DiskRoot : DiskRecord
	{
		public DiskRoot() : base("Disks") { }

		static HashSet<string> paths = new HashSet<string>();

		public static void EnsureShareExists(string uri)
		{
			paths.Add(uri.ToLowerInvariant());
		}

		public override IEnumerable<Record> Records
		{
			get
			{
				foreach (var drive in DriveInfo.GetDrives())
					yield return new DiskDir(drive.Name.Substring(0, drive.Name.Length - 1).ToUpper());
				foreach (var path in paths)
					yield return new NetworkShare(path);
			}
		}
	}
}
