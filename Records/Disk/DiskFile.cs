using System;
using System.IO;

namespace NeoEdit.Records.Disk
{
	public class DiskFile : RecordItem
	{
		public DiskFile(string uri) : base(uri)
		{
			var fileInfo = new FileInfo(FullName);
			Size = fileInfo.Length;
		}

		public override RecordList Parent { get { return new DiskDir(Path.GetDirectoryName(FullName)); } }

		public override byte[] Read(Int64 position, int bytes)
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
