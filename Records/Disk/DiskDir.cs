using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace NeoEdit.Records.Disk
{
	public class DiskDir : DiskRecord
	{
		public DiskDir(string uri, Record parent)
			: base(uri, parent)
		{
			if (new Regex("^[a-zA-Z]:$").IsMatch(uri))
				this[RecordProperty.PropertyName.Name] = FullName;
		}

		Regex rootRE = new Regex("^[a-zA-Z]:$");
		bool IsRoot()
		{
			return rootRE.IsMatch(FullName);
		}

		protected override IEnumerable<Record> InternalRecords
		{
			get
			{
				var find = FullName + (IsRoot() ? @"\" : "");
				foreach (var dir in Directory.EnumerateDirectories(find))
					yield return new DiskDir(dir, this);
				foreach (var file in Directory.EnumerateFiles(find))
					yield return new DiskFile(file, this);
			}
		}
	}
}
