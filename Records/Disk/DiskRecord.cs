using System;
using System.IO;

namespace NeoEdit.Records.Disk
{
	public abstract class DiskRecord : Record
	{
		public DiskRecord(string uri, Record parent) : base(uri, parent) { }

		public override void Rename(string newName, System.Func<bool> canOverwrite)
		{
			newName = Path.Combine(Path.GetDirectoryName(FullName), newName);
			if (Directory.Exists(newName))
				throw new Exception("A directory already exists at the specified location.");

			if (File.Exists(newName))
			{
				if (!canOverwrite())
					return;

				File.Delete(newName);
				Parent.RemoveChild(newName);
			}

			if (this is DiskDir)
				Directory.Move(FullName, newName);
			else if (this is DiskFile)
				File.Move(FullName, newName);
			FullName = newName;
		}
	}
}
