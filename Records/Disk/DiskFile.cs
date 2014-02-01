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

		protected override void SetProperty<T>(RecordProperty.PropertyName property, T value)
		{
			base.SetProperty<T>(property, value);
			switch (property)
			{
				case RecordProperty.PropertyName.Name:
					this[RecordProperty.PropertyName.NameWoExtension] = Path.GetFileNameWithoutExtension(value as string);
					this[RecordProperty.PropertyName.Extension] = Path.GetExtension(value as string);
					break;
			}
		}

		public override void Delete()
		{
			File.Delete(FullName);
			Parent.RemoveChild(this);
		}
	}
}
