using System.IO;

namespace NeoEdit.DiskModule.Local
{
	public class Disk : IDisk
	{
		public IDir GetDirectory(string directory)
		{
			while (!string.IsNullOrWhiteSpace(directory))
			{
				if (Directory.Exists(directory))
					return new Dir(directory);
				directory = Path.GetDirectoryName(directory);
			}
			return new Dir(null);
		}
	}
}
