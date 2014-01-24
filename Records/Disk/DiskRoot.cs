using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace NeoEdit.Records.Disk
{
	public class DiskRoot : RecordRoot
	{
		public DiskRoot(Record parent) : base("Disks", parent) { }

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

		protected override IEnumerable<Tuple<string, Func<string, Record>>> InternalRecords
		{
			get
			{
				foreach (var drive in DriveInfo.GetDrives())
					yield return new Tuple<string, Func<string, Record>>(drive.Name.Substring(0, drive.Name.Length - 1).ToUpper(), a => new DiskDir(a, this));
			}
		}
	}
}
