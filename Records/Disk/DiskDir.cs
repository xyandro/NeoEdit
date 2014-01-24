using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace NeoEdit.Records.Disk
{
	public class DiskDir : DiskRecord
	{
		public DiskDir(string uri, Record parent)
			: base(uri, parent)
		{
			if (new Regex("^[a-zA-Z]:$").IsMatch(uri))
				this[Property.PropertyType.Name] = FullName;
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

		protected override IEnumerable<Tuple<string, Func<string, Record>>> InternalRecords
		{
			get
			{
				SetupWatcher();
				var find = FullName + (IsRoot() ? @"\" : "");
				foreach (var dir in Directory.EnumerateDirectories(find))
					yield return new Tuple<string, Func<string, Record>>(dir, a => new DiskDir(a, this));
				foreach (var file in Directory.EnumerateFiles(find))
					yield return new Tuple<string, Func<string, Record>>(file, a => new DiskFile(a, this));
			}
		}
	}
}
