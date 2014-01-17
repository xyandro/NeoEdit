using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace NeoEdit.Records.Disk
{
	public class DiskDir : RecordList
	{
		public DiskDir(string uri, RecordList parent)
			: base(uri, parent)
		{
			if (new Regex("^[a-zA-Z]:$").IsMatch(uri))
				this[Property.Name] = FullName;
		}

		Regex rootRE = new Regex("^[a-zA-Z]:$");
		bool IsRoot()
		{
			return rootRE.IsMatch(FullName);
		}

		FileSystemWatcher watcher;
		void SetupWatcher()
		{
			if (watcher != null)
				return;

			watcher = new FileSystemWatcher();
			watcher.Path = FullName;
			//watcher.NotifyFilter = NotifyFilters.LastWrite;
			watcher.Changed += (o, a) => Refresh();
			watcher.Created += (o, a) => Refresh();
			watcher.Deleted += (o, a) => Refresh();
			watcher.Renamed += (o, a) => Refresh();
			watcher.EnableRaisingEvents = true;
		}

		protected override IEnumerable<Record> InternalRecords
		{
			get
			{
				SetupWatcher();
				var find = FullName + (IsRoot() ? @"\" : "");
				foreach (var dir in Directory.EnumerateDirectories(find))
					yield return new DiskDir(dir, this);
				foreach (var file in Directory.EnumerateFiles(find))
					yield return new DiskFile(file, this);
			}
		}
	}
}
