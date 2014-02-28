using System.Collections.Generic;
using System.IO;
using NeoEdit.Records.Zipped;

namespace NeoEdit.Records.Disk
{
	public class DiskDir : DiskRecord
	{
		public DiskDir(string uri) : base(uri) { }

		public override IEnumerable<Record> Records
		{
			get
			{
				var find = FullName;
				if (find.Length == 2)
					find += @"\";
				foreach (var dir in Directory.EnumerateDirectories(find))
					yield return new DiskDir(dir);
				foreach (var file in Directory.EnumerateFiles(find))
				{
					if (Path.GetExtension(file).Equals(".zip", System.StringComparison.OrdinalIgnoreCase))
						yield return new ZippedArchive(file);
					else
						yield return new DiskFile(file);
				}
			}
		}

		public override bool CanPaste() { return true; }
		public override bool CanRename() { return true; }
		public override bool CanDelete() { return true; }
		public override bool CanCopy() { return true; }
		public override bool CanCut() { return true; }
		public override bool CanSyncSource() { return true; }
		public override bool CanSyncTarget() { return true; }

		public override void Delete()
		{
			Directory.Delete(FullName, true);
		}

		public override Record CreateFile(string name)
		{
			return new DiskFile(Path.Combine(FullName, name));
		}

		public override Record CreateDirectory(string name)
		{
			name = Path.Combine(FullName, name);
			Directory.CreateDirectory(name);
			return new DiskDir(name);
		}

		public override void MoveFrom(Record source, string newName = null)
		{
			if (source is DiskDir)
			{
				Directory.Move(source.FullName, Path.Combine(FullName, newName ?? source.Name));
				return;
			}

			if (source is DiskFile)
			{
				File.Move(source.FullName, Path.Combine(FullName, newName ?? source.Name));
				return;
			}

			base.MoveFrom(source, newName);
		}

	}
}
