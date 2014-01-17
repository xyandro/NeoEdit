using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace NeoEdit.Records.Disk
{
	public class DiskDir : IRecordList
	{
		public IRecordList Parent
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

		public string Name { get; private set; }
		public string FullName { get; private set; }
		public IEnumerable<IRecord> Records
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

		public DiskDir(string uri)
		{
			FullName = uri;
			Name = Path.GetFileName(FullName);
			if (new Regex("^[a-zA-Z]:$").IsMatch(uri))
				Name = FullName;
		}
	}
}
