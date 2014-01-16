using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NeoEdit.Records.Disk
{
	public class DiskDir : IRecordList
	{
		static string FixCasing(string name)
		{
			var ret = name;
			var parts = ret.Split('\\').ToList();
			ret = parts[0].ToUpper() + @"\";
			parts.RemoveAt(0);

			foreach (var part in parts)
			{
				var next = Directory.GetFileSystemEntries(ret).SingleOrDefault(a => Path.GetFileName(a).Equals(part, StringComparison.OrdinalIgnoreCase));
				if (next == null)
					throw new Exception(String.Format("Invalid registry key: {0}", name));
				ret = next;
			}

			return ret;
		}

		static Func<String, IRecordList> Provider
		{
			get
			{
				return name =>
				{
					name = name.Replace("/", "\\");
					while (true)
					{
						var oldName = name;
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

					name = FixCasing(name);
					return new DiskDir(name);
				};
			}
		}

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
	}
}
