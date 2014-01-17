using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace NeoEdit.Records.Disk
{
	public class DiskRoot : RecordRoot
	{
		public DiskRoot(RecordList parent) : base("Disks", parent) { }

		public override Record GetRecord(string uri)
		{
			uri = uri.Replace("/", "\\");
			uri = uri.Replace("\"", "");
			uri = uri.Trim();
			uri = uri.TrimEnd('\\');
			var netPath = uri.StartsWith(@"\\");
			uri = new Regex("\\\\+").Replace(uri, "\\");

			if ((File.Exists(uri)) || (Directory.Exists(uri)))
				return base.GetRecord(uri);
			return null;
		}

		public override IEnumerable<Record> Records
		{
			get
			{
				foreach (var drive in DriveInfo.GetDrives())
					yield return new DiskDir(drive.Name.Substring(0, drive.Name.Length - 1).ToUpper(), this);
			}
		}
	}
}
