using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeoEdit.LocalDisk
{
	public class Disk
	{
		public Dir RootDirectory()
		{
			return new Dir(null);
		}

		public Dir GetDirectory(string directory)
		{
			while (!string.IsNullOrWhiteSpace(directory))
			{
				if (Directory.Exists(directory))
					return new Dir(directory);
				directory = Path.GetDirectoryName(directory);
			}
			return RootDirectory();
		}
	}
}
