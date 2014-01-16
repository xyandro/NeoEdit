using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace NeoEdit.Records.Disk
{
	public class DiskDir : IRecordList
	{
		static Func<String, IRecordList> Provider
		{
			get
			{
				return name =>
				{
					while (true)
					{
						var oldName = name;
						name = name.Replace("/", "\\");
						name = name.Replace("\\\\", "\\");
						name = name.Trim().TrimEnd('\\');
						if ((name.StartsWith("\"")) && (name.EndsWith("\"")))
							name = name.Substring(1, name.Length - 2);
						if (oldName == name)
							break;
					}
					if (File.Exists(name))
						name = Path.GetDirectoryName(name);
					else if (!Directory.Exists(name))
						return null;

					name = Helpers.GetWindowsPhysicalPath(name);
					return new DiskDir(name);
				};
			}
		}

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
