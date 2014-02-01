using System.Collections.Generic;

namespace NeoEdit.Records.Zipped
{
	class ZippedDir : ZippedRecord
	{
		public ZippedDir(string uri, Record parent, string archive) : base(uri, parent, archive) { }

		protected override IEnumerable<Record> InternalRecords
		{
			get
			{
				var path = FullName.Substring(archive.Length + 1).Replace(@"\", "/") + "/";
				return GetFiles(this, archive, path);
			}
		}
	}
}
