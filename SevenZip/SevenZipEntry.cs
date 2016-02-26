using System;
using System.IO;

namespace NeoEdit.SevenZip
{
	public class SevenZipEntry
	{
		public uint Index { get; }
		public string Path { get; }
		public bool IsDirectory { get; }
		public long Size { get; }
		public long PackedSize { get; }
		public DateTime LastWriteTime { get; }

		readonly SevenZipArchive archive;
		internal SevenZipEntry(SevenZipArchive archive, uint index)
		{
			this.archive = archive;
			Index = index;
			Path = archive.GetProperty<string>(index, PropID.Path);
			IsDirectory = archive.GetProperty<bool>(index, PropID.IsDirectory);
			Size = archive.GetProperty<long>(index, PropID.Size);
			PackedSize = archive.GetProperty<long>(index, PropID.PackedSize);
			LastWriteTime = archive.GetProperty<DateTime>(index, PropID.LastWriteTime);
		}

		public void Extract(Stream stream) => archive.Extract(Index, stream);

		public void Extract(string fileName)
		{
			using (var stream = File.Create(fileName))
				Extract(stream);
		}
	}
}
