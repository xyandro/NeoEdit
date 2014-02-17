using System;
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

		void Paste()
		{
			List<Record> myRecords;
			bool isCut;
			Clipboard.Current.GetRecords(out myRecords, out isCut);
			if (myRecords == null)
				return;

			foreach (var child in myRecords)
			{
				var name = child[RecordProperty.PropertyName.NameWoExtension] as string;
				var ext = child[RecordProperty.PropertyName.Extension] as string;
				string newName;
				for (var num = 1; ; ++num)
				{
					var extra = num == 1 ? "" : String.Format(" ({0})", num);
					newName = Path.Combine(FullName, name + extra + ext);
					if ((File.Exists(newName)) || (Directory.Exists(newName)))
					{
						if (isCut)
							throw new Exception("Destination already exists.");
						continue;
					}
					break;
				}

				if (isCut)
				{
					if (child is DiskFile)
						File.Move(child.FullName, newName);
					else if (child is DiskDir)
						Directory.Move(child.FullName, newName);
				}
				else
				{
					if (child is DiskFile)
						File.Copy(child.FullName, newName);
					else if (child is DiskDir)
						CopyDir(child.FullName, newName);
				}
			}
		}

		public override void Sync(Record source)
		{
			var destFiles = Records;
			var sourceFiles = source.Records;

			var recordsToDelete = destFiles.Where(a => !sourceFiles.Any(b => a.Name == b.Name)).ToList();
			var recordsToCopy = sourceFiles.Where(a => !destFiles.Any(b => a.Name == b.Name)).ToList();

			foreach (var record in recordsToDelete)
				record.Delete();

			foreach (var record in recordsToCopy)
			{
				if (record is DiskFile)
					File.Copy(record.FullName, Path.Combine(FullName, record.Name));
				else if (record is DiskDir)
					Directory.CreateDirectory(Path.Combine(FullName, record.Name));
			}

			foreach (var record in Records)
			{
				var sourceRecord = sourceFiles.Single(a => a.Name == record.Name);
				record.Sync(sourceRecord);
			}
		}

		public override Record CreateDirectory(string name)
		{
			name = Path.Combine(FullName, name);
			Directory.CreateDirectory(name);
			return new DiskDir(name);
		}
	}
}
