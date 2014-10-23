using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Common;
using SevenZip;

namespace NeoEdit.Disk
{
	public class DiskItem : DependencyObject
	{
		[DepProp]
		public string FullName { get { return UIHelper<DiskItem>.GetPropValue<string>(this); } private set { UIHelper<DiskItem>.SetPropValue(this, value); } }
		[DepProp]
		public string Path { get { return UIHelper<DiskItem>.GetPropValue<string>(this); } private set { UIHelper<DiskItem>.SetPropValue(this, value); } }
		[DepProp]
		public string Name { get { return UIHelper<DiskItem>.GetPropValue<string>(this); } private set { UIHelper<DiskItem>.SetPropValue(this, value); } }
		[DepProp]
		public string NameWoExtension { get { return UIHelper<DiskItem>.GetPropValue<string>(this); } private set { UIHelper<DiskItem>.SetPropValue(this, value); } }
		[DepProp]
		public string Extension { get { return UIHelper<DiskItem>.GetPropValue<string>(this); } private set { UIHelper<DiskItem>.SetPropValue(this, value); } }
		[DepProp]
		public long? Size { get { return UIHelper<DiskItem>.GetPropValue<long?>(this); } private set { UIHelper<DiskItem>.SetPropValue(this, value); } }
		[DepProp]
		public DateTime? WriteTime { get { return UIHelper<DiskItem>.GetPropValue<DateTime?>(this); } private set { UIHelper<DiskItem>.SetPropValue(this, value); } }
		[DepProp]
		public DateTime? CreateTime { get { return UIHelper<DiskItem>.GetPropValue<DateTime?>(this); } private set { UIHelper<DiskItem>.SetPropValue(this, value); } }
		[DepProp]
		public DateTime? AccessTime { get { return UIHelper<DiskItem>.GetPropValue<DateTime?>(this); } private set { UIHelper<DiskItem>.SetPropValue(this, value); } }
		[DepProp]
		public string MD5 { get { return UIHelper<DiskItem>.GetPropValue<string>(this); } private set { UIHelper<DiskItem>.SetPropValue(this, value); } }
		[DepProp]
		public string SHA1 { get { return UIHelper<DiskItem>.GetPropValue<string>(this); } private set { UIHelper<DiskItem>.SetPropValue(this, value); } }
		[DepProp]
		public string Identity { get { return UIHelper<DiskItem>.GetPropValue<string>(this); } private set { UIHelper<DiskItem>.SetPropValue(this, value); } }
		[DepProp]
		public long? CompressedSize { get { return UIHelper<DiskItem>.GetPropValue<long?>(this); } private set { UIHelper<DiskItem>.SetPropValue(this, value); } }

		public bool IsDiskItem { get { return Parent.contentItem.type == DiskItemType.Disk; } }
		public bool HasChildren { get; private set; }
		public bool HasData { get; private set; }

		public enum DiskItemType
		{
			None,
			Disk,
			SevenZipArchive,
		}

		public readonly DiskItem Parent;
		readonly DiskItem contentItem;
		readonly DiskItemType type;

		static DiskItem()
		{
			UIHelper<DiskItem>.Register();
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
		{
			FullName = fullName;
			HasChildren = isDir;
			HasData = !isDir;
			Parent = _parent;
			type = DiskItemType.None;
			if (FullName == "")
				type = DiskItemType.Disk;

			Path = GetPath(FullName);
			Name = FullName.Substring(Path.Length);
			if ((Name.StartsWith(@"\")) && (Path != ""))
				Name = Name.Substring(1);
			var idx = Name.LastIndexOf('.');
			NameWoExtension = idx == -1 ? Name : Name.Substring(0, idx);
			Extension = idx == -1 ? "" : Name.Substring(idx).ToLowerInvariant();

			if (IsSevenZipArchiveType())
				type = DiskItemType.SevenZipArchive;

			if (type != DiskItemType.None)
				HasChildren = true;

			for (contentItem = this; contentItem != null; contentItem = contentItem.Parent)
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

		static string GetPath(string fullName)
		{
			if (fullName == "")
				return "";

			var idx = fullName.LastIndexOf('\\');
			if ((idx == -1) || ((fullName.StartsWith(@"\\")) && (idx < 2)))
				return "";

			return fullName.Substring(0, idx);
		}

		string NameRelativeTo(DiskItem item)
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

		public static readonly DiskItem Root = new DiskItem("", true, null);

		static bool IsChildOf(string path, string parent)
		{
			if ((parent == "") || (path == parent))
				return true;
			return path.StartsWith(parent + @"\");
		}

		public static string Simplify(string path)
		{
			var network = path.StartsWith(@"\\");
			path = Regex.Replace(path.Trim().Trim('"'), @"[\\/]+", @"\").TrimEnd('\\');
			if (network)
			{
				path = @"\\" + path.TrimStart('\\');
				EnsureShareExists(path);
			}
			return path;
		}

		FilePath GetFilePath(bool needStream)
		{
			switch (Parent.contentItem.type)
			{
				case DiskItemType.Disk:
					return new FilePath(FullName, needStream ? File.OpenRead(FullName) : null);
				case DiskItemType.SevenZipArchive:
					{
						var fileName = System.IO.Path.GetTempFileName();
						var stream = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.Delete, 512, FileOptions.DeleteOnClose);
						using (var filePath = Parent.contentItem.GetFilePath(true))
						using (var zip = new SevenZipExtractor(filePath.Stream))
						{
							if (Parent.contentItem.IsSevenZipNoName())
								zip.ExtractFile(0, stream);
							else
								zip.ExtractFile(NameRelativeTo(Parent.contentItem), stream);

							stream.Position = 0;
						}
						return new FilePath(fileName, stream);
					}
				default: throw new NotImplementedException();
			}
		}

		public IEnumerable<DiskItem> GetChildren()
		{
			switch (contentItem.type)
			{
				case DiskItemType.Disk: return GetDiskChildren();
				case DiskItemType.SevenZipArchive: return GetSevenZipChildren();
				default: throw new Exception("Can't get children");
			}
		}

		IEnumerable<DiskItem> GetDiskChildren()
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

		IEnumerable<DiskItem> GetSevenZipChildren()
		{
			using (var filePath = contentItem.GetFilePath(true))
			using (var zip = new SevenZipExtractor(filePath.Stream))
			{
				var found = new HashSet<string>();
				var contentName = NameRelativeTo(contentItem);
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

			using (var filePath = GetFilePath(false))
				Identity = Identifier.Identify(filePath.Path);
		}

		public void CalcMD5()
		{
			if (!HasData)
				return;

			using (var filePath = GetFilePath(true))
				MD5 = Checksum.Get(Checksum.Type.MD5, filePath.Stream);
		}

		public void CalcSHA1()
		{
			if (!HasData)
				return;

			using (var filePath = GetFilePath(true))
			SHA1 = Checksum.Get(Checksum.Type.SHA1, filePath.Stream);
		}

		public void MoveFrom(DiskItem item, string newName = null)
		{
			if ((!IsDiskItem) || (!item.IsDiskItem) || (HasData))
				throw new Exception("Can only move disk between disk locations.");

			var newFullName = System.IO.Path.Combine(FullName, newName ?? item.Name);

			if ((File.Exists(newFullName)) || (Directory.Exists(newFullName)))
				throw new Exception("A file or directory with that name already exists.");

			if (item.HasData)
				File.Move(item.FullName, newFullName);
			else
				Directory.Move(item.FullName, newFullName);
		}

		public void CopyFrom(DiskItem item, string newName = null)
		{
			newName = newName ?? item.Name;
			if (!item.HasData)
			{
				var dest = CreateDirectory(newName);
				dest.SyncFrom(item);
				return;
			}

			var newFullName = System.IO.Path.Combine(FullName, newName);
			if ((File.Exists(newFullName)) || (Directory.Exists(newFullName)))
				throw new Exception("A file or directory with that name already exists.");
			if (item.IsDiskItem)
			{
				System.IO.File.Copy(item.FullName, newFullName);
				return;
			}

			using (var input = item.GetFilePath(true))
			using (var output = File.Create(newFullName))
				input.Stream.CopyTo(output);
		}

		public DiskItem CreateFile(string name)
		{
			return new DiskItem(System.IO.Path.Combine(FullName, name), false, this);
		}

		public DiskItem CreateDirectory(string name)
		{
			name = System.IO.Path.Combine(FullName, name);
			Directory.CreateDirectory(name);
			return new DiskItem(name, true, this);
		}

		string GetKey()
		{
			long ticks = 0;
			var time = WriteTime;
			if (time.HasValue)
				ticks = time.Value.Ticks;
			return String.Format("{0}-{1}-{2}-{3}", HasData, Name, Size, ticks);
		}

		void SyncFrom(DiskItem source)
		{
			if (HasData != source.HasData)
				throw new Exception("Can't sync files to directories.");
			if (HasData)
				return;

			var sourceRecords = new Dictionary<string, DiskItem>();
			foreach (DiskItem child in source.GetChildren())
				sourceRecords[child.GetKey()] = child;

			var destRecords = new Dictionary<string, DiskItem>();
			foreach (DiskItem child in GetChildren())
				destRecords[child.GetKey()] = child;

			var dups = sourceRecords.Keys.Where(key => destRecords.Keys.Contains(key)).ToList();
			foreach (var item in dups)
			{
				destRecords[item].SyncFrom(sourceRecords[item]);
				sourceRecords.Remove(item);
				destRecords.Remove(item);
			}

			foreach (var item in destRecords)
				item.Value.Delete();

			foreach (var item in sourceRecords.Values)
				CopyFrom(item);
		}

		public void Delete()
		{
			if (HasData)
				File.Delete(FullName);
			else
				Directory.Delete(FullName, true);
		}

		public static DiskItem Get(string fullName)
		{
			var result = Root;
			if (String.IsNullOrEmpty(fullName))
				return result;

			fullName = DiskItem.Simplify(fullName);

			var parts = new List<string>();
			while (fullName != "")
			{
				parts.Insert(0, fullName);
				fullName = GetPath(fullName);
			}

			foreach (var part in parts)
			{
				if ((result == null) || (!result.HasChildren))
					return null;
				result = result.GetChildren().FirstOrDefault(child => child.FullName.Equals(part, StringComparison.InvariantCultureIgnoreCase));
			}

			return result;
		}

		public override string ToString() { return FullName; }
	}
}
