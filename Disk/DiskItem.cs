using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.ItemGridControl;

namespace NeoEdit.Disk
{
	public class DiskItem : ItemGridTreeItem<DiskItem>
	{
		[DepProp]
		public string Path { get { return GetValue<string>(); } protected set { SetValue(value); } }
		[DepProp]
		public string Name { get { return GetValue<string>(); } protected set { SetValue(value); } }
		[DepProp]
		public string NameWoExtension { get { return GetValue<string>(); } protected set { SetValue(value); } }
		[DepProp]
		public string Extension { get { return GetValue<string>(); } protected set { SetValue(value); } }
		[DepProp]
		public long? Size { get { return GetValue<long?>(); } protected set { SetValue(value); } }
		[DepProp]
		public DateTime? WriteTime { get { return GetValue<DateTime?>(); } protected set { SetValue(value); } }
		[DepProp]
		public DateTime? CreateTime { get { return GetValue<DateTime?>(); } protected set { SetValue(value); } }
		[DepProp]
		public DateTime? AccessTime { get { return GetValue<DateTime?>(); } protected set { SetValue(value); } }
		[DepProp]
		public string MD5 { get { return GetValue<string>(); } protected set { SetValue(value); } }
		[DepProp]
		public string Identity { get { return GetValue<string>(); } protected set { SetValue(value); } }
		[DepProp]
		public long? CompressedSize { get { return GetValue<long?>(); } protected set { SetValue(value); } }

		public bool IsDir { get; private set; }
		public bool IsZip { get { return Extension == ".zip"; } }
		readonly DiskSource source;
		DiskItem(DiskSource _source, string fullName, bool isDir)
			: base(fullName)
		{
			source = _source;
			IsDir = isDir;
			var idx = FullName.LastIndexOf('\\');
			Path = idx == -1 ? "" : FullName.Substring(0, idx);
			Name = idx == -1 ? FullName : FullName.Substring(idx + 1);
			idx = Name.LastIndexOf('.');
			NameWoExtension = idx == -1 ? Name : "";
			Extension = idx == -1 ? "" : Name.Substring(idx).ToLowerInvariant();
		}

		public static DiskItem GetRoot()
		{
			return new DiskItem(new DiskSource(null, "", DiskSource.DiskSourceType.Disk), "", true);
		}

		static bool IsChildOf(string path, string parent)
		{
			if ((parent == "") || (path == parent))
				return true;
			return path.StartsWith(parent + @"\");
		}

		public static string Simplify(string path)
		{
			return Regex.Replace(path.Trim().Trim('"'), @"[\\/]+", @"\");
		}

		string GetRemaining(string first)
		{
			if (!IsChildOf(FullName, first))
				throw new ArgumentException();
			var remaining = FullName.Substring(first.Length);
			if (remaining.StartsWith(@"\"))
				remaining = remaining.Substring(1);
			return remaining;
		}

		public override IItemGridTreeItem GetParent()
		{
			if (FullName == "")
				return null;

			var useSource = source;
			while (!IsChildOf(Path, useSource.Path))
				useSource = useSource.Parent;
			return new DiskItem(useSource, Path, true);
		}

		public override bool CanGetChildren()
		{
			return (IsDir) || (IsZip);
		}

		public override IEnumerable<IItemGridTreeItem> GetChildren()
		{
			if (IsDir)
				return GetDirChildren();

			if (IsZip)
				return GetZipChildren(new DiskSource(source, FullName, DiskSource.DiskSourceType.ZipArchive));

			throw new Exception("Can't get children");
		}

		IEnumerable<IItemGridTreeItem> GetDirChildren()
		{
			switch (source.Type)
			{
				case DiskSource.DiskSourceType.Disk: return GetDiskChildren();
				case DiskSource.DiskSourceType.ZipArchive: return GetZipChildren();
			}
			throw new Exception("Can't get children");
		}

		IEnumerable<IItemGridTreeItem> GetDiskChildren()
		{
			if ((source.Type == DiskSource.DiskSourceType.Disk) && (FullName == ""))
			{
				foreach (var drive in DriveInfo.GetDrives())
					yield return new DiskItem(source, drive.Name.Substring(0, drive.Name.Length - 1).ToUpper(), true);
			}
			else if (source.Type == DiskSource.DiskSourceType.Disk)
			{
				var find = FullName;
				if (find.Length == 2)
					find += @"\";
				foreach (var dir in Directory.EnumerateDirectories(find))
					yield return new DiskItem(source, dir, true);
				foreach (var file in Directory.EnumerateFiles(find))
					yield return new DiskItem(source, file, false);
			}
		}

		IEnumerable<IItemGridTreeItem> GetZipChildren(DiskSource source = null)
		{
			if (source == null)
				source = this.source;

			var path = GetRemaining(source.Path);
			if (path.Length != 0)
				path += @"\";

			var stream = source.GetStream();
			var archive = new ZipArchive(stream, ZipArchiveMode.Read);
			var found = new HashSet<string>();
			foreach (var entry in archive.Entries)
			{
				var name = entry.FullName.Replace("/", @"\");
				if (!name.StartsWith(path))
					continue;
				name = name.Substring(path.Length);
				var idx = name.IndexOf('\\');
				var isDir = false;
				if (idx != -1)
				{
					name = name.Substring(0, idx);
					if (found.Contains(name))
						continue;
					found.Add(name);
					isDir = true;
				}

				yield return new DiskItem(source, FullName + @"\" + name, isDir);
			}
		}
	}
}
