using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
			using (var zipFile = ZipFile.OpenRead(archive.FullName))
			{
				var entry = zipFile.GetEntry(InArchiveName);
				if (entry != null)
				{
					this[RecordProperty.PropertyName.Size] = entry.Length;
					this[RecordProperty.PropertyName.CompressedSize] = entry.CompressedLength;
					this[RecordProperty.PropertyName.WriteTime] = entry.LastWriteTime.UtcDateTime;
				}
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

		public override IEnumerable<RecordAction.ActionName> Actions
		{
			get
			{
				return new List<RecordAction.ActionName> { 
					RecordAction.ActionName.MD5,
					RecordAction.ActionName.Open,
				}.Concat(base.Actions);
			}
		}

		public override BinaryData Read()
		{
			using (var zipFile = ZipFile.OpenRead(archive.FullName))
			{
				var entry = zipFile.GetEntry(InArchiveName);
				using (var stream = entry.Open())
				using (var ms = new MemoryStream())
				{
					stream.CopyTo(ms);
					return ms.ToArray();
				}
			}
		}

		public override void Write(BinaryData data)
		{
			using (var zipFile = ZipFile.Open(archive.FullName, ZipArchiveMode.Update))
			{
				var entry = zipFile.CreateEntry(InArchiveName);
				using (var stream = entry.Open())
				{
					var bytes = data.GetAllBytes();
					stream.Write(bytes, 0, bytes.Length);
				}
			}
		}

		public override void CalcMD5()
		{
			using (var md5 = MD5.Create())
				this[RecordProperty.PropertyName.MD5] = Checksum.Get(Checksum.Type.MD5, Read().GetAllBytes());
		}

		public override void Delete()
		{
			using (var zipFile = ZipFile.Open(archive.FullName, ZipArchiveMode.Update))
			{
				var entry = zipFile.GetEntry(InArchiveName);
				entry.Delete();
			}
		}

		public override void SyncFrom(Record source)
		{
			if (source is Disk.DiskFile)
			{
				using (var zipFile = ZipFile.Open(archive.FullName, ZipArchiveMode.Update))
					zipFile.CreateEntryFromFile(source.FullName, InArchiveName);
				return;
			}

			base.SyncFrom(source);
		}
	}
}
