using System;
using System.IO;

namespace NeoEdit.Records.Disk
{
	public class DiskFile : IRecordItem
	{
		public IRecordList Parent { get { return new DiskDir(Path.GetDirectoryName(FullName)); } }
		public string Name { get { return Path.GetFileName(FullName); } }
		public string FullName { get; private set; }
		public long Size { get; private set; }

		public DiskFile(string fullName)
		{
			FullName = fullName;
			var fileInfo = new FileInfo(FullName);
			Size = fileInfo.Length;
		}

		public byte[] Read(Int64 position, int bytes)
		{
			var file = File.OpenRead(FullName);
			bytes = (int)Math.Max(0, Math.Min(bytes, file.Length - position));
			var data = new byte[bytes];
			file.Position = position;
			file.Read(data, 0, bytes);
			return data;
		}
	}
}
