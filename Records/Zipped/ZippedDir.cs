using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;

namespace NeoEdit.Records.Zipped
{
	class ZippedDir : ZippedRecord
	{
		public ZippedDir(string uri, Record parent, string archive) : base(uri, parent, archive) { }

		public override IEnumerable<RecordAction.ActionName> Actions
		{
			get
			{
				return new List<RecordAction.ActionName> { 
					RecordAction.ActionName.Delete,
				}.Concat(base.Actions);
			}
		}

		protected override IEnumerable<Record> InternalRecords
		{
			get
			{
				var path = FullName.Substring(archive.Length + 1).Replace(@"\", "/") + "/";
				return GetFiles(this, archive, path);
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
			RemoveFromParent();
		}
	}
}
