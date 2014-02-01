using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace NeoEdit.Records.Zipped
{
	public class ZippedRecord : Record
	{
		readonly protected string archive;
		public ZippedRecord(string uri, Record parent, string _archive)
			: base(uri, parent)
		{
			archive = _archive;
		}

		public static IEnumerable<ZippedRecord> GetFiles(Record parent, string archive, string path)
		{
			using (var zipFile = ZipFile.OpenRead(archive))
			{
				var found = new Dictionary<string, ZippedRecord>();
				foreach (var file in zipFile.Entries)
				{
					var name = file.FullName;
					if (!name.StartsWith(path))
						continue;
					name = name.Substring(path.Length);
					if (String.IsNullOrEmpty(name))
						continue;
					var isDir = false;
					var split = name.IndexOf('/');
					if (split >= 0)
					{
						name = name.Substring(0, split);
						isDir = true;
					}
					if (found.ContainsKey(name))
						continue;

					var fullName = Path.Combine(parent.FullName, name);

					if (isDir)
						found[name] = new ZippedDir(fullName, parent, archive);
					else
						found[name] = new ZippedFile(fullName, parent, archive);
				}
				return found.Values.OrderBy(a => a.FullName);
			}
		}
	}
}
