using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeoEdit.LocalDisk
{
	public class Dir
	{
		public string Name { get; private set; }
		public bool Root { get; private set; }
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
