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
				if (parent == null)
					return new DiskRoot();
				return new DiskDir(parent);
			}
		}
		public string Name { get; private set; }
		public string FullName { get; private set; }
		public IEnumerable<IRecord> Records
		{
			get
			{
				foreach (var dir in Directory.EnumerateDirectories(FullName))
					yield return new DiskDir(dir);
				foreach (var file in Directory.EnumerateFiles(FullName))
					yield return new DiskFile(file);
			}
		}

		public DiskDir(string fullName)
		{
			FullName = fullName;
			Name = Path.GetFileName(FullName);
			if (new Regex("^[a-zA-Z]:\\\\$").IsMatch(fullName))
				Name = FullName;
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
