using System;
using System.IO;

namespace NeoEdit.Records.Disk
{
	public class DiskFile : Record
	{
		public DiskFile(string uri, Record parent)
			: base(uri, parent)
		{
			var fileInfo = new FileInfo(FullName);
			this[Property.PropertyType.Size] = fileInfo.Length;
			this[Property.PropertyType.WriteTime] = fileInfo.LastWriteTimeUtc;
			this[Property.PropertyType.CreateTime] = fileInfo.CreationTimeUtc;
			this[Property.PropertyType.AccessTime] = fileInfo.LastAccessTimeUtc;
		}

		public override bool IsFile { get { return true; } }
	}
}
