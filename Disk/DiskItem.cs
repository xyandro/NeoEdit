using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Management;
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

		public bool HasChildren { get; private set; }
		public bool HasData { get; private set; }

		public enum DiskItemType
		{
			None,
			Disk,
			ZipArchive,
			GZipArchive,
		}

		readonly DiskItem parent, contentItem;
		readonly DiskItemType type;

		DiskItem(string fullName, bool isDir, DiskItem _parent)
			: base(fullName)
		{
			HasChildren = isDir;
			HasData = !isDir;
			parent = _parent;
			type = DiskItemType.None;
			if (fullName == "")
				type = DiskItemType.Disk;

			Path = GetPath(FullName);
			Name = fullName.Substring(Path.Length);
			if ((Name.StartsWith(@"\")) && (Path != ""))
				Name = Name.Substring(1);
			var idx = Name.LastIndexOf('.');
			NameWoExtension = idx == -1 ? Name : "";
			Extension = idx == -1 ? "" : Name.Substring(idx).ToLowerInvariant();

			switch (Extension)
			{
				case ".zip":
					type = DiskItemType.ZipArchive;
					break;
				case ".gz":
					type = DiskItemType.GZipArchive;
					break;
			}

			if (type != DiskItemType.None)
				HasChildren = true;

			for (contentItem = this; contentItem != null; contentItem = contentItem.parent)
				if (contentItem.type != DiskItemType.None)
					break;
		}

		protected override string GetPath(string fullName)
		{
			if (fullName == "")
				return "";

			var idx = fullName.LastIndexOf('\\');
			if ((idx == -1) || ((fullName.StartsWith(@"\\")) && (idx < 2)))
				return "";

			return fullName.Substring(0, idx);
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

		static HashSet<string> shares = new HashSet<string>();
		static void EnsureShareExists(string path)
		{
			var idx = path.IndexOf('\\', 2);
			if (idx == -1)
				idx = path.Length;
			var share = path.Substring(0, idx).ToLowerInvariant();
			shares.Add(share);
		}

		public static DiskItem GetRoot()
		{
			return new DiskItem("", true, null);
		}

		static bool IsChildOf(string path, string parent)
		{
			if ((parent == "") || (path == parent))
				return true;
			return path.StartsWith(parent + @"\");
		}

		public static string Simplify(string path)
		{
			var network = path.StartsWith(@"\\");
			path = Regex.Replace(path.Trim().Trim('"'), @"[\\/]+", @"\");
			if (network)
			{
				path = @"\" + path;
				EnsureShareExists(path);
			}
			return path;
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
				case DiskItemType.GZipArchive:
					return new GZipStream(parent.contentItem.GetStream(), CompressionMode.Decompress);
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
			return HasChildren;
		}

		public override IEnumerable<IItemGridTreeItem> GetChildren()
		{
			switch (contentItem.type)
			{
				case DiskItemType.Disk: return GetDiskChildren();
				case DiskItemType.ZipArchive: return GetZipChildren();
				case DiskItemType.GZipArchive: return new List<IItemGridTreeItem> { new DiskItem(FullName + @"\" + Name.Substring(0, Name.Length - 3), false, this) };
				default: throw new Exception("Can't get children");
			}
		}

		IEnumerable<IItemGridTreeItem> GetDiskChildren()
		{
			if (FullName == "")
			{
				foreach (var drive in DriveInfo.GetDrives())
					yield return new DiskItem(drive.Name.Substring(0, drive.Name.Length - 1).ToUpper(), true, this);
				foreach (var share in shares)
					yield return new DiskItem(share, true, this);
			}
			else if ((FullName.StartsWith(@"\\")) && (Path == ""))
			{
				using (var shares = new ManagementClass(FullName + @"\root\cimv2", "Win32_Share", new ObjectGetOptions()))
					foreach (var share in shares.GetInstances())
						yield return new DiskItem(FullName + @"\" + share["Name"], true, this);
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
			if (!HasData)
				return;

			using (var name = GetFileName())
				Identity = Identifier.Identify(name.Path);
		}

		public override string ToString() { return type.ToString() + ": " + FullName; }
	}
}
