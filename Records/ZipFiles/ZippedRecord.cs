using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NeoEdit.Records.ZipFiles
{
	public class ZippedRecord : Record
	{
		readonly protected ZippedArchive archive;
		public ZippedRecord(string uri, ZippedArchive _archive)
			: base(uri)
		{
			archive = _archive;
		}

		public override bool CanDelete() { return true; }
		public override bool CanRename() { return true; }
		public override bool CanCopy() { return true; }
		public override bool CanCut() { return true; }
		public override bool CanSyncSource() { return true; }
		public override bool CanSyncTarget() { return true; }

		public override Record Parent
		{
			get
			{
				var parent = GetProperty<string>(RecordProperty.PropertyName.Path);
				if (parent == archive.FullName)
					return archive;
				return new ZippedDir(parent, archive);
			}
		}

		protected string InArchiveName { get { return FullName.Substring(archive.FullName.Length + 1).Replace('\\', '/'); } }

		public static IEnumerable<ZippedRecord> GetFiles(string parentFullName, ZippedArchive archive, string path)
		{
			var zipFile = archive.Open();
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
