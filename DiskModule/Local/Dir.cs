using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NeoEdit.DiskModule.Local
{
	public class Dir : IDir
	{
		public string Name { get; private set; }
		public List<string> Files { get; private set; }

		public Dir(string name)
		{
			Name = name;
			if (Name == null)
			{
				Name = "Root";
				Files = DriveInfo.GetDrives().Select(a => a.RootDirectory.Name).ToList();
			}
			else
			{
				Files = Directory.EnumerateFileSystemEntries(name).ToList();
			}
		}
	}
}
