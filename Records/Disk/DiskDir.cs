using System.Collections.Generic;
using System.IO;
using System.Linq;

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
					yield return new DiskFile(file);
			}
		}

		public override IEnumerable<RecordAction.ActionName> Actions
		{
			get { return new List<RecordAction.ActionName> { RecordAction.ActionName.Paste, }.Concat(base.Actions); }
		}

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

		public override void Move(Record destination, string newName = null)
		{
			if (destination is DiskDir)
			{
				newName = Path.Combine(destination.FullName, newName ?? Name);
				Directory.Move(FullName, newName);
				FullName = newName;
				return;
			}

			base.Move(destination);
		}
	}
}
