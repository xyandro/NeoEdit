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
		public string Path { get { return GetValue<string>(); } private set { SetValue(value); } }
		[DepProp]
		public string Name { get { return GetValue<string>(); } private set { SetValue(value); } }
		[DepProp]
		public string NameWoExtension { get { return GetValue<string>(); } private set { SetValue(value); } }
		[DepProp]
		public string Extension { get { return GetValue<string>(); } private set { SetValue(value); } }
		[DepProp]
		public long? Size { get { return GetValue<long?>(); } private set { SetValue(value); } }
		[DepProp]
		public DateTime? WriteTime { get { return GetValue<DateTime?>(); } private set { SetValue(value); } }
		[DepProp]
		public DateTime? CreateTime { get { return GetValue<DateTime?>(); } private set { SetValue(value); } }
		[DepProp]
		public DateTime? AccessTime { get { return GetValue<DateTime?>(); } private set { SetValue(value); } }
		[DepProp]
		public string MD5 { get { return GetValue<string>(); } private set { SetValue(value); } }
		[DepProp]
		public string Identity { get { return GetValue<string>(); } private set { SetValue(value); } }
		[DepProp]
		public long? CompressedSize { get { return GetValue<long?>(); } private set { SetValue(value); } }

		public bool IsDir { get; private set; }
		public bool IsZip { get { return Extension == ".zip"; } }

		public enum DiskItemType
		{
			None,
			Disk,
			ZipArchive,
		}

		readonly DiskItem parent, contentItem;
		readonly DiskItemType type;

		DiskItem(string fullName, bool isDir, DiskItem _parent, DiskItemType _type = DiskItemType.None)
			: base(fullName)
		{
			IsDir = isDir;
			parent = _parent;
			type = _type;

			for (contentItem = this; contentItem != null; contentItem = contentItem.parent)
				if (contentItem.type != DiskItemType.None)
					break;

			var idx = FullName.LastIndexOf('\\');
			Path = idx == -1 ? "" : FullName.Substring(0, idx);
			Name = idx == -1 ? FullName : FullName.Substring(idx + 1);
			idx = Name.LastIndexOf('.');
			NameWoExtension = idx == -1 ? Name : "";
			Extension = idx == -1 ? "" : Name.Substring(idx).ToLowerInvariant();
		}

		string GetRelativeName(DiskItem item)
		{
			var name = item == null ? "" : item.FullName;
			if (name == FullName)
				return "";
			if (name != "")
				name += @"\";
			if (!FullName.StartsWith(name))
				throw new ArgumentException();
			return FullName.Substring(name.Length);
		}

		public static DiskItem GetRoot()
		{
			return new DiskItem("", true, null, DiskItemType.Disk);
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

		public Stream GetStream()
		{
			var name = GetRelativeName(parent.contentItem);
			switch (parent.contentItem.type)
			{
				case DiskItemType.Disk: return File.OpenRead(name);
				case DiskItemType.ZipArchive:
					{
						var zip = new ZipArchive(parent.contentItem.GetStream(), ZipArchiveMode.Read);
						var entry = zip.GetEntry(name.Replace(@"\", "/"));
						var stream = entry.Open();
						return stream;
					}
				default: throw new NotImplementedException();
			}
		}

		FilePath GetFileName()
		{
			switch (contentItem.type)
			{
				case DiskItemType.Disk: return new FilePath(FullName);
				default: return new FilePath(GetStream());
			}
		}

		public override IItemGridTreeItem GetParent()
		{
			return parent;
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
			{
				var item = new DiskItem(FullName, true, parent, DiskItemType.ZipArchive);
				return item.GetChildren();
			}

			throw new Exception("Can't get children");
		}

		IEnumerable<IItemGridTreeItem> GetDirChildren()
		{
			switch (contentItem.type)
			{
				case DiskItemType.Disk: return GetDiskChildren();
				case DiskItemType.ZipArchive: return GetZipChildren();
			}
			throw new Exception("Can't get children");
		}

		IEnumerable<IItemGridTreeItem> GetDiskChildren()
		{
			if (FullName == "")
			{
				foreach (var drive in DriveInfo.GetDrives())
					yield return new DiskItem(drive.Name.Substring(0, drive.Name.Length - 1).ToUpper(), true, this);
			}
			else
			{
				var find = FullName;
				if (find.Length == 2)
					find += @"\";
				foreach (var dir in Directory.EnumerateDirectories(find))
					yield return new DiskItem(dir, true, this);
				foreach (var file in Directory.EnumerateFiles(find))
					yield return new DiskItem(file, false, this);
			}
		}

		IEnumerable<IItemGridTreeItem> GetZipChildren()
		{
			var stream = contentItem.GetStream();
			var archive = new ZipArchive(stream, ZipArchiveMode.Read);
			var found = new HashSet<string>();
			var contentName = GetRelativeName(contentItem);
			if (contentName != "")
				contentName += @"\";
			foreach (var entry in archive.Entries)
			{
				var name = entry.FullName.Replace("/", @"\");
				if (!name.StartsWith(contentName))
					continue;
				name = name.Substring(contentName.Length);
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

				yield return new DiskItem(FullName + @"\" + name, isDir, this);
			}
		}

		public void Identify()
		{
			if (IsDir)
				return;

			using (var name = GetFileName())
				Identity = Identifier.Identify(name.Path);
		}

		public override string ToString() { return type.ToString() + ": " + FullName; }
	}
}
