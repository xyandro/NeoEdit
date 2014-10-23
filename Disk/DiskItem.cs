using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Common;

namespace NeoEdit.Disk
{
	public class DiskItem : DependencyObject
	{
		public enum DiskItemType
		{
			None,
			Root,
			Share,
			Directory,
			File,
		}

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
		public DiskItemType Type { get { return UIHelper<DiskItem>.GetPropValue<DiskItemType>(this); } private set { UIHelper<DiskItem>.SetPropValue(this, value); } }

		public bool HasChildren { get { return Type != DiskItemType.File; } }
		public DiskItem Parent { get { return new DiskItem(Path); } }

		static DiskItem() { UIHelper<DiskItem>.Register(); }

		public DiskItem(string fullName)
		{
			FullName = fullName ?? "";
			Path = GetPath(FullName);
			Name = FullName.Substring(Path.Length);
			if ((Name.StartsWith(@"\")) && (Path != ""))
				Name = Name.Substring(1);
			var idx = Name.LastIndexOf('.');
			NameWoExtension = idx == -1 ? Name : Name.Substring(0, idx);
			Extension = idx == -1 ? "" : Name.Substring(idx).ToLowerInvariant();
			Type = DiskItemType.None;

			if ((Type == DiskItemType.None) && (String.IsNullOrEmpty(FullName)))
				Type = DiskItemType.Root;

			if ((Type == DiskItemType.None) && (FullName.StartsWith(@"\\")) && (Path == ""))
				Type = DiskItemType.Share;

			if (Type == DiskItemType.None)
			{
				var dirInfo = new DirectoryInfo(FullName);
				if (dirInfo.Exists)
				{
					WriteTime = dirInfo.LastWriteTimeUtc;
					CreateTime = dirInfo.CreationTimeUtc;
					AccessTime = dirInfo.LastAccessTimeUtc;
					Type = DiskItemType.Directory;
				}
			}

			if (Type == DiskItemType.None)
			{
				var fileInfo = new FileInfo(FullName);
				if (fileInfo.Exists)
				{
					Size = fileInfo.Length;
					WriteTime = fileInfo.LastWriteTimeUtc;
					CreateTime = fileInfo.CreationTimeUtc;
					AccessTime = fileInfo.LastAccessTimeUtc;
					Type = DiskItemType.File;
				}
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

		static HashSet<string> shares = new HashSet<string>();
		static void EnsureShareExists(string path)
		{
			var idx = path.IndexOf('\\', 2);
			if (idx == -1)
				idx = path.Length;
			var share = path.Substring(0, idx).ToLowerInvariant();
			shares.Add(share);
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

		public IEnumerable<DiskItem> GetChildren()
		{
			switch (Type)
			{
				case DiskItemType.Root: return GetRootChildren();
				case DiskItemType.Share: return GetShareChildren();
				case DiskItemType.Directory: return GetDirectoryChildren();
				default: throw new Exception("Can't get children");
			}
		}

		IEnumerable<DiskItem> GetRootChildren()
		{
			foreach (var drive in DriveInfo.GetDrives())
				yield return new DiskItem(drive.Name.Substring(0, drive.Name.Length - 1).ToUpper());
			foreach (var share in shares)
				yield return new DiskItem(share);
		}

		IEnumerable<DiskItem> GetShareChildren()
		{
			using (var shares = new ManagementClass(FullName + @"\root\cimv2", "Win32_Share", new ObjectGetOptions()))
				foreach (var share in shares.GetInstances())
					yield return new DiskItem(FullName + @"\" + share["Name"]);
		}

		IEnumerable<DiskItem> GetDirectoryChildren()
		{
			var find = FullName;
			if (find.Length == 2)
				find += @"\";
			foreach (var entry in Directory.EnumerateFileSystemEntries(find))
				yield return new DiskItem(entry);
		}

		public void Identify()
		{
			if (Type != DiskItemType.File)
				return;

			Identity = Identifier.Identify(FullName);
		}

		public void CalcMD5()
		{
			if (Type != DiskItemType.File)
				return;

			var data = File.ReadAllBytes(FullName);
			MD5 = Checksum.Get(Checksum.Type.MD5, data);
		}

		public void CalcSHA1()
		{
			if (Type != DiskItemType.File)
				return;

			var data = File.ReadAllBytes(FullName);
			SHA1 = Checksum.Get(Checksum.Type.SHA1, data);
		}

		public void MoveFrom(DiskItem item, string newName = null)
		{
			var newFullName = System.IO.Path.Combine(FullName, newName ?? item.Name);

			if ((File.Exists(newFullName)) || (Directory.Exists(newFullName)))
				throw new Exception("A file or directory with that name already exists.");

			switch (Type)
			{
				case DiskItemType.File: File.Move(item.FullName, newFullName); break;
				case DiskItemType.Directory: Directory.Move(item.FullName, newFullName); break;
				default: throw new Exception("Can only move file and directories.");
			}
		}

		public void CopyFrom(DiskItem item, string newName = null)
		{
			newName = newName ?? item.Name;
			if (item.Type == DiskItemType.Directory)
			{
				var name = System.IO.Path.Combine(FullName, newName);
				Directory.CreateDirectory(name);
				new DiskItem(name).SyncFrom(item);
				return;
			}

			var newFullName = System.IO.Path.Combine(FullName, newName);
			if ((File.Exists(newFullName)) || (Directory.Exists(newFullName)))
				throw new Exception("A file or directory with that name already exists.");
			if (item.Type == DiskItemType.File)
			{
				File.Copy(item.FullName, newFullName);
				return;
			}
		}

		string GetKey()
		{
			long ticks = 0;
			var time = WriteTime;
			if (time.HasValue)
				ticks = time.Value.Ticks;
			return String.Format("{0}-{1}-{2}-{3}", Type, Name, Size, ticks);
		}

		void SyncFrom(DiskItem source)
		{
			if (Type != source.Type)
				throw new Exception("Can't sync files to directories.");
			if (Type == DiskItemType.File)
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
			switch (Type)
			{
				case DiskItemType.File: File.Delete(FullName); break;
				case DiskItemType.Directory: Directory.Delete(FullName, true); break;
				default: throw new ArgumentException("Can only delete files and directories");
			}
		}

		public static DiskItem Get(string fullName)
		{
			var result = new DiskItem(null);
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
