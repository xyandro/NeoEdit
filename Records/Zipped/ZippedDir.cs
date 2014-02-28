using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NeoEdit.GUI.Records.Zipped
{
	class ZippedDir : ZippedRecord
	{
		public ZippedDir(string uri, ZippedArchive archive) : base(uri, archive) { }

		public override Record CreateFile(string name) { return new ZippedFile(Path.Combine(FullName, name), archive); }
		public override Record CreateDirectory(string name) { return new ZippedDir(Path.Combine(FullName, name), archive); }

		public override IEnumerable<Record> Records
		{
			get
			{
				var path = FullName.Substring(archive.FullName.Length + 1).Replace(@"\", "/") + "/";
				return GetFiles(FullName, archive, path);
			}
		}

		public override void Delete()
		{
			var name = InArchiveName + "/";
			var zipFile = archive.Open(true);
			var entries = zipFile.Entries.Where(a => a.FullName.StartsWith(name)).ToList();
			entries.ForEach(a => a.Delete());
		}
	}
}
