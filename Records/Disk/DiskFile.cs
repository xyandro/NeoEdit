using System;
using System.IO;

namespace NeoEdit.Records.Disk
{
	public class DiskFile : DiskRecord
	{
		public DiskFile(string uri, Record parent)
			: base(uri, parent)
		{
			var fileInfo = new FileInfo(FullName);
			this[RecordProperty.PropertyName.Size] = fileInfo.Length;
			this[RecordProperty.PropertyName.WriteTime] = fileInfo.LastWriteTimeUtc;
			this[RecordProperty.PropertyName.CreateTime] = fileInfo.CreationTimeUtc;
			this[RecordProperty.PropertyName.AccessTime] = fileInfo.LastAccessTimeUtc;
		}

		public override bool IsFile { get { return true; } }

		public override string FullName
		{
			get { return base.FullName; }
			protected set
			{
				base.FullName = value;
				this[RecordProperty.PropertyName.Extension] = Path.GetExtension(FullName);
			}
		}
	}
}
