using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace NeoEdit.Records.Disk
{
	public class DiskDir : RecordList
	{
		public DiskDir(string uri)
			: base(uri)
		{
			if (new Regex("^[a-zA-Z]:$").IsMatch(uri))
				Name = FullName;
		}

		public override RecordList Parent
		{
			get
			{
				var parent = Path.GetDirectoryName(FullName);
				if (String.IsNullOrEmpty(parent))
					return new DiskRoot();
				return new DiskDir(parent);
			}
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
					yield return new DiskDir(dir);
				foreach (var file in Directory.EnumerateFiles(find))
					yield return new DiskFile(file);
			}
		}
	}
}
