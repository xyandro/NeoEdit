using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using NeoEdit.Records.Disk;

namespace NeoEdit.Records.Zipped
{
	public class ZippedArchive : DiskFile
	{
		public ZippedArchive(string uri) : base(uri) { }

		public override IEnumerable<Record> Records { get { return ZippedRecord.GetFiles(FullName, this, ""); } }

		public override Record CreateFile(string name) { return new ZippedFile(Path.Combine(FullName, name), this); }
		public override Record CreateDirectory(string name) { return new ZippedDir(Path.Combine(FullName, name), this); }

		static Dictionary<string, WeakReference<ZipArchive>> zipArchiveCache = new Dictionary<string, WeakReference<ZipArchive>>();
		void ClearUnreferencedCache()
		{
			var keys = zipArchiveCache.Keys.ToList();
			foreach (var key in keys)
			{
				ZipArchive archive;
				if (!zipArchiveCache[key].TryGetTarget(out archive))
					zipArchiveCache.Remove(key);
			}
		}

		internal ZipArchive Open(bool write = false)
		{
			ClearUnreferencedCache();

			ZipArchive archive;
			if (zipArchiveCache.ContainsKey(FullName))
				if (zipArchiveCache[FullName].TryGetTarget(out archive))
				{
					if ((archive.Mode == ZipArchiveMode.Update) == write)
						return archive;

					archive.Dispose();
				}

			archive = ZipFile.Open(FullName, write ? ZipArchiveMode.Update : ZipArchiveMode.Read);
			zipArchiveCache[FullName] = new WeakReference<ZipArchive>(archive);
			return archive;
		}

		internal void Release()
		{
			if (!zipArchiveCache.ContainsKey(FullName))
				return;

			ZipArchive archive;
			if (!zipArchiveCache[FullName].TryGetTarget(out archive))
				return;

			archive.Dispose();
			zipArchiveCache.Remove(FullName);
		}
	}
}
