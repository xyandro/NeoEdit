using System;
using System.IO;
using System.IO.Compression;

namespace NeoEdit.Disk
{
	public class DiskSource
	{
		public enum DiskSourceType
		{
			Disk,
			ZipArchive,
		}

		public DiskSource Parent { get; private set; }
		public DiskSourceType Type { get; private set; }
		public string Path { get; private set; }
		public DiskSource(DiskSource parent, string path, DiskSourceType type)
		{
			Parent = parent;
			Path = path;
			Type = type;
		}

		public Stream GetStream()
		{
			switch (Parent.Type)
			{
				case DiskSourceType.Disk: return File.OpenRead(Path);
				case DiskSourceType.ZipArchive:
					{
						var zip = new ZipArchive(Parent.GetStream(), ZipArchiveMode.Read);
						var entry = zip.GetEntry(Path.Substring(Parent.Path.Length + 1).Replace(@"\", "/"));
						var stream = entry.Open();
						return stream;
					}
			}

			throw new NotImplementedException();
		}
	}
}
