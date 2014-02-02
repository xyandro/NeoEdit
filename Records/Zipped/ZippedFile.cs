using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;

namespace NeoEdit.Records.Zipped
{
	class ZippedFile : ZippedRecord
	{
		public ZippedFile(string uri, Record parent, string archive) : base(uri, parent, archive) { }
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

		private string InArchiveName { get { return FullName.Substring(archive.Length + 1).Replace('\\', '/'); } }
		public override byte[] Read()
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
				this[RecordProperty.PropertyName.MD5] = BitConverter.ToString(md5.ComputeHash(Read())).Replace("-", "").ToLower();
		}
	}
}
