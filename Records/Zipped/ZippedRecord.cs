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
		public ZippedRecord(string uri, string _archive)
			: base(uri)
		{
			archive = _archive;
		}

		public override Record Parent
		{
			get
			{
				var parent = Path.GetDirectoryName(FullName);
				if (parent == archive)
					return new Disk.DiskFile(parent);
				return new ZippedDir(parent, archive);
			}
		}

		protected string InArchiveName { get { return FullName.Substring(archive.Length + 1).Replace('\\', '/'); } }

		public static IEnumerable<ZippedRecord> GetFiles(string parentFullName, string archive, string path)
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

					var fullName = Path.Combine(parentFullName, name);

					if (isDir)
						found[name] = new ZippedDir(fullName, archive);
					else
						found[name] = new ZippedFile(fullName, archive);
				}
				return found.Values.OrderBy(a => a.FullName);
			}
		}
	}
}
