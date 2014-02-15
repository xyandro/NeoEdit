using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;

namespace NeoEdit.Records.Zipped
{
	class ZippedDir : ZippedRecord
	{
		public ZippedDir(string uri, string archive) : base(uri, archive) { }

		public override IEnumerable<RecordAction.ActionName> Actions
		{
			get
			{
				return new List<RecordAction.ActionName> { 
					RecordAction.ActionName.Delete,
				}.Concat(base.Actions);
			}
		}

		public override IEnumerable<Record> Records
		{
			get
			{
				var path = FullName.Substring(archive.Length + 1).Replace(@"\", "/") + "/";
				return GetFiles(FullName, archive, path);
			}
		}

		public override void Delete()
		{
			var name = InArchiveName + "/";
			using (var zipFile = ZipFile.Open(archive, ZipArchiveMode.Update))
			{
				var entries = zipFile.Entries.Where(a => a.FullName.StartsWith(name)).ToList();
				entries.ForEach(a => a.Delete());
			}
		}
	}
}
