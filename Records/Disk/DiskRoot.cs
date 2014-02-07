using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace NeoEdit.Records.Disk
{
	public class DiskRoot : RecordRoot
	{
		public static DiskRoot Static { get; private set; }

		internal DiskRoot(Record parent)
			: base("Disks", parent)
		{
			if (Static != null)
				throw new Exception("Can only create root nodes once.");
			Static = this;
		}

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

		protected override IEnumerable<Record> InternalRecords
		{
			get
			{
				foreach (var drive in DriveInfo.GetDrives())
					yield return new DiskDir(drive.Name.Substring(0, drive.Name.Length - 1).ToUpper(), this);
			}
		}
	}
}
