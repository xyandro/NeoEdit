using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace NeoEdit.Records.Zipped
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
			using (var zipFile = ZipFile.Open(archive.FullName, ZipArchiveMode.Update))
			{
				var entries = zipFile.Entries.Where(a => a.FullName.StartsWith(name)).ToList();
				entries.ForEach(a => a.Delete());
			}
		}

		public override void SyncFrom(Record source, string newName = null)
		{
			if (!ZippedRecord.SyncFrom(archive, source, InArchiveName + "/" + newName))
				base.SyncFrom(source, newName);
		}
	}
}
