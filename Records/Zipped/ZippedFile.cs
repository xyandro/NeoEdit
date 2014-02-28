using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using NeoEdit.Common;
using NeoEdit.Data;

namespace NeoEdit.Records.Zipped
{
	class ZippedFile : ZippedRecord
	{
		public ZippedFile(string uri, ZippedArchive archive)
			: base(uri, archive)
		{
			var zipFile = archive.Open();
			var entry = zipFile.GetEntry(InArchiveName);
			if (entry != null)
			{
				this[RecordProperty.PropertyName.Size] = entry.Length;
				this[RecordProperty.PropertyName.CompressedSize] = entry.CompressedLength;
				this[RecordProperty.PropertyName.WriteTime] = entry.LastWriteTime.UtcDateTime;
			}
		}

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

		public override bool IsFile { get { return true; } }
		public override bool CanMD5() { return true; }
		public override bool CanOpen() { return true; }

		public override BinaryData Read()
		{
			var zipFile = archive.Open();
			var entry = zipFile.GetEntry(InArchiveName);
			using (var stream = entry.Open())
			using (var ms = new MemoryStream())
			{
				stream.CopyTo(ms);
				return new MemoryBinaryData(ms.ToArray());
			}
		}

		public override void Write(BinaryData data)
		{
			var zipFile = archive.Open(true);
			var entry = zipFile.CreateEntry(InArchiveName);
			using (var stream = entry.Open())
			{
				var bytes = data.GetAllBytes();
				stream.Write(bytes, 0, bytes.Length);
			}
		}

		public override void CalcMD5()
		{
			using (var md5 = MD5.Create())
				this[RecordProperty.PropertyName.MD5] = Checksum.Get(Checksum.Type.MD5, Read().GetAllBytes());
		}

		public override void Delete()
		{
			var zipFile = archive.Open(true);
			var entry = zipFile.GetEntry(InArchiveName);
			entry.Delete();
		}

		protected override void CopyFrom(Record source)
		{
			if (source is Disk.DiskFile)
			{
				var zipFile = archive.Open(true);
				zipFile.CreateEntryFromFile(source.FullName, InArchiveName);
				return;
			}

			base.CopyFrom(source);
		}
	}
}
