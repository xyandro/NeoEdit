﻿using System.Collections.Generic;
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
		public ZippedFile(string uri, string archive)
			: base(uri, archive)
		{
			using (var zipFile = ZipFile.OpenRead(archive))
			{
				var entry = zipFile.GetEntry(InArchiveName);
				this[RecordProperty.PropertyName.Size] = entry.Length;
				this[RecordProperty.PropertyName.CompressedSize] = entry.CompressedLength;
				this[RecordProperty.PropertyName.WriteTime] = entry.LastWriteTime.UtcDateTime;
			}
		}

		public override bool IsFile { get { return true; } }

		public override IEnumerable<RecordAction.ActionName> Actions
		{
			get
			{
				return new List<RecordAction.ActionName> { 
					RecordAction.ActionName.MD5,
					RecordAction.ActionName.Delete,
					RecordAction.ActionName.Open,
				}.Concat(base.Actions);
			}
		}

		public override BinaryData Read()
		{
			using (var zipFile = ZipFile.OpenRead(archive))
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

		public override void CalcMD5()
		{
			using (var md5 = MD5.Create())
				this[RecordProperty.PropertyName.MD5] = Checksum.Get(Checksum.Type.MD5, Read().GetAllBytes());
		}

		public override void Delete()
		{
			using (var zipFile = ZipFile.Open(archive, ZipArchiveMode.Update))
			{
				var entry = zipFile.GetEntry(InArchiveName);
				entry.Delete();
			}
		}
	}
}
