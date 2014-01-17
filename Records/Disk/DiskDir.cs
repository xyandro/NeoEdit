using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace NeoEdit.Records.Disk
{
	public class DiskDir : RecordList
	{
		public DiskDir(string uri, RecordList parent)
			: base(uri, parent)
		{
			if (new Regex("^[a-zA-Z]:$").IsMatch(uri))
				Name = FullName;
		}

		Regex rootRE = new Regex("^[a-zA-Z]:$");
		bool IsRoot()
		{
			return rootRE.IsMatch(FullName);
		}

		public override IEnumerable<Record> Records
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
