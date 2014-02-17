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

		void CopyDir(string oldName, string newName)
		{
			Directory.CreateDirectory(newName);

			foreach (var file in Directory.EnumerateFiles(oldName))
			{
				var name = Path.GetFileName(file);
				File.Copy(Path.Combine(oldName, name), Path.Combine(newName, name));
			}

			foreach (var dir in Directory.EnumerateDirectories(oldName))
			{
				var name = Path.GetFileName(dir);
				CopyDir(Path.Combine(oldName, name), Path.Combine(newName, name));
			}
		}

		public override Record CreateDirectory(string name)
		{
			name = Path.Combine(FullName, name);
			Directory.CreateDirectory(name);
			return new DiskDir(name);
		}

		public override void Rename(string newName)
		{
			newName = Path.Combine(GetProperty<string>(RecordProperty.PropertyName.Path), newName);
			Directory.Move(FullName, newName);
			FullName = newName;
		}

		public override void Move(Record destination)
		{
			if (destination is DiskDir)
			{
				var newName = Path.Combine(destination.FullName, Name);
				Directory.Move(FullName, newName);
				FullName = newName;
				return;
			}

			base.Move(destination);
		}
	}
}
