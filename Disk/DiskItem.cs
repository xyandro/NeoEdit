using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Reflection;
using System.Text.RegularExpressions;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.ItemGridControl;
using SevenZip;

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
		public string SHA1 { get { return GetValue<string>(); } private set { SetValue(value); } }
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
			SevenZipArchive,
		}

		readonly DiskItem parent, contentItem;
		readonly DiskItemType type;

		static DiskItem()
		{
			using (var input = typeof(DiskItem).Assembly.GetManifestResourceStream("NeoEdit.Disk.7z.dll"))
			{
				byte[] data;
				using (var ms = new MemoryStream())
				{
					input.CopyTo(ms);
					data = ms.ToArray();
				}
				data = Compression.Decompress(Compression.Type.GZip, data);
				File.WriteAllBytes(System.IO.Path.GetFullPath(System.IO.Path.Combine(typeof(DiskItem).Assembly.Location, "..", "7z.dll")), data);
			}
		}

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
			NameWoExtension = idx == -1 ? Name : Name.Substring(0, idx);
			Extension = idx == -1 ? "" : Name.Substring(idx).ToLowerInvariant();

			if (IsSevenZipArchiveType())
				type = DiskItemType.SevenZipArchive;

			if (type != DiskItemType.None)
				HasChildren = true;

			for (contentItem = this; contentItem != null; contentItem = contentItem.parent)
				if (contentItem.type != DiskItemType.None)
					break;
		}

		static DiskItem FromFile(string fullName, DiskItem parent)
		{
			var item = new DiskItem(fullName, false, parent);
			var fileInfo = new FileInfo(item.FullName);
			if (fileInfo.Exists)
			{
				item.Size = fileInfo.Length;
				item.WriteTime = fileInfo.LastWriteTimeUtc;
				item.CreateTime = fileInfo.CreationTimeUtc;
				item.AccessTime = fileInfo.LastAccessTimeUtc;
			}
			return item;
		}

		static DiskItem FromDirectory(string fullName, DiskItem parent)
		{
			var item = new DiskItem(fullName, true, parent);
			var dirInfo = new DirectoryInfo(item.FullName);
			if (dirInfo.Exists)
			{
				item.WriteTime = dirInfo.LastWriteTimeUtc;
				item.CreateTime = dirInfo.CreationTimeUtc;
				item.AccessTime = dirInfo.LastAccessTimeUtc;
			}
			return item;
		}

		static DiskItem FromSevenZip(string fullName, bool isDir, DiskItem parent, ArchiveFileInfo info)
		{
			var item = new DiskItem(fullName, isDir, parent);
			if (!isDir)
			{
				item.CompressedSize = (long)info.PackedSize;
				item.Size = (long)info.Size;
				item.AccessTime = info.LastAccessTime;
				item.WriteTime = info.LastWriteTime;
				item.CreateTime = info.CreationTime;
			}
			return item;
		}

		bool IsSevenZipArchiveType()
		{
			switch (Extension)
			{
				case ".7z":
				case ".xz":
				case ".txz":
				case ".bz":
				case ".bz2":
				case ".bzip2":
				case ".tbz":
				case ".tar":
				case ".zip":
				case ".wim":
				case ".arj":
				case ".cab":
				case ".chm":
				case ".cpio":
				case ".cramfs":
				case ".deb":
				case ".dmg":
				case ".fat":
				case ".hfs":
				case ".iso":
				case ".lzh":
				case ".lzma":
				case ".mbr":
				case ".msi":
				case ".nsis":
				case ".ntfs":
				case ".rar":
				case ".rpm":
				case ".squashfs":
				case ".udf":
				case ".vhd":
				case ".xar":
				case ".z":
				case ".gz":
				case ".gzip":
				case ".tgz":
					return true;
				default:
					return false;
			}
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
				case DiskItemType.SevenZipArchive:
					{
						var stream = new FileStream(System.IO.Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.Delete, 512, FileOptions.DeleteOnClose);
						using (var zip = new SevenZipExtractor(parent.contentItem.GetStream()))
						{
							if (parent.contentItem.IsSevenZipNoName())
								zip.ExtractFile(0, stream);
							else
								zip.ExtractFile(name, stream);

							stream.Position = 0;
							return stream;
						}
					}
				default: throw new NotImplementedException();
			}
		}

		FilePath GetFileName()
		{
			switch (contentItem.type)
			{
				case DiskItemType.Disk: return new FilePath(FullName);
				default:
					using (var stream = GetStream())
						return new FilePath(stream);
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
				case DiskItemType.SevenZipArchive: return GetSevenZipChildren();
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
					yield return FromDirectory(dir, this);
				foreach (var file in Directory.EnumerateFiles(find))
					yield return FromFile(file, this);
			}
		}

		bool IsSevenZipNoName()
		{
			switch (Extension)
			{
				case ".xz":
				case ".txz":
				case ".bz":
				case ".bz2":
				case ".bzip2":
				case ".tbz":
				case ".gz":
				case ".gzip":
				case ".tgz":
					return true;
				default: return false;
			}
		}

		IEnumerable<IItemGridTreeItem> GetSevenZipChildren()
		{
			using (var stream = contentItem.GetStream())
			using (var zip = new SevenZipExtractor(stream))
			{
				var found = new HashSet<string>();
				var contentName = GetRelativeName(contentItem);
				if (contentName != "")
					contentName += @"\";
				var noName = IsSevenZipNoName();
				foreach (var entry in zip.ArchiveFileData)
				{
					var name = entry.FileName;
					if (noName)
					{
						name = NameWoExtension;
						if ((Extension == ".tgz") || (Extension == ".tbz") || (Extension == ".txz"))
							name += ".tar";
					}
					if (!name.StartsWith(contentName))
						continue;
					name = name.Substring(contentName.Length);
					var idx = name.IndexOf('\\');
					var isDir = entry.IsDirectory;
					if (idx != -1)
					{
						name = name.Substring(0, idx);
						isDir = true;
					}
					if (found.Contains(name))
						continue;
					found.Add(name);

					yield return FromSevenZip(FullName + @"\" + name, isDir, this, entry);
				}
			}
		}

		public void Identify()
		{
			if (!HasData)
				return;

			using (var name = GetFileName())
				Identity = Identifier.Identify(name.Path);
		}

		public void CalcMD5()
		{
			if (!HasData)
				return;

			MD5 = Checksum.Get(Checksum.Type.MD5, GetStream());
		}

		public void CalcSHA1()
		{
			if (!HasData)
				return;

			SHA1 = Checksum.Get(Checksum.Type.SHA1, GetStream());
		}

		public override string ToString() { return type.ToString() + ": " + FullName; }
	}
}
