using System.Collections.Generic;
using System.IO;
using NeoEdit.Records.Disk;

namespace NeoEdit.Records.Zipped
{
	public class ZippedArchive : DiskFile
	{
		public ZippedArchive(string uri) : base(uri) { }

		public override IEnumerable<Record> Records { get { return ZippedRecord.GetFiles(FullName, this, ""); } }

		public override Record CreateFile(string name) { return new ZippedFile(Path.Combine(FullName, name), this); }
		public override Record CreateDirectory(string name) { return new ZippedDir(Path.Combine(FullName, name), this); }
	}
}
