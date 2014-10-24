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
		public string FullName { get { return UIHelper<DiskItem>.GetPropValue<string>(this); } set { UIHelper<DiskItem>.SetPropValue(this, value ?? ""); SetFullName(); } }
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

		public bool HasChildren { get { return type != DiskItemType.File; } }
		public DiskItem Parent { get { return new DiskItem(Path); } }

		public bool CanRename { get { return (type == DiskItemType.File) || (type == DiskItemType.Directory); } }

		public bool Exists
		{
			get
			{
				switch (type)
				{
					case DiskItemType.None: return false;
					case DiskItemType.Directory: return Directory.Exists(FullName);
					case DiskItemType.File: return File.Exists(FullName);
					case DiskItemType.Root: return true;
					case DiskItemType.Share: return true;
					default: throw new ArgumentException("Invalid Type");
				}
			}
		}

		DiskItemType type;

		static DiskItem() { UIHelper<DiskItem>.Register(); }

		public DiskItem(string fullName)
		{
			FullName = fullName;
			Refresh();
		}

		void SetFullName()
		{
			Path = GetPath(FullName);
			Name = FullName.Substring(Path.Length);
			if ((Name.StartsWith(@"\")) && (Path != ""))
				Name = Name.Substring(1);
			var idx = Name.LastIndexOf('.');
			NameWoExtension = idx == -1 ? Name : Name.Substring(0, idx);
			Extension = idx == -1 ? "" : Name.Substring(idx).ToLowerInvariant();
		}

		public void Refresh()
		{
			Size = null;
			WriteTime = CreateTime = AccessTime = null;
			type = DiskItemType.None;

			if ((type == DiskItemType.None) && (String.IsNullOrEmpty(FullName)))
				type = DiskItemType.Root;

			if ((type == DiskItemType.None) && (FullName.StartsWith(@"\\")) && (Path == ""))
				type = DiskItemType.Share;

			if (type == DiskItemType.None)
			{
				var dirInfo = new DirectoryInfo(FullName);
				if (dirInfo.Exists)
				{
					WriteTime = dirInfo.LastWriteTimeUtc;
					CreateTime = dirInfo.CreationTimeUtc;
					AccessTime = dirInfo.LastAccessTimeUtc;
					type = DiskItemType.Directory;
				}
			}

			if (type == DiskItemType.None)
			{
				var fileInfo = new FileInfo(FullName);
				if (fileInfo.Exists)
				{
					Size = fileInfo.Length;
					WriteTime = fileInfo.LastWriteTimeUtc;
					CreateTime = fileInfo.CreationTimeUtc;
					AccessTime = fileInfo.LastAccessTimeUtc;
					type = DiskItemType.File;
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
			switch (type)
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
			if (type != DiskItemType.File)
				return;

			Identity = Identifier.Identify(FullName);
		}

		public void CalcMD5()
		{
			if (type != DiskItemType.File)
				return;

			var data = File.ReadAllBytes(FullName);
			MD5 = Checksum.Get(Checksum.Type.MD5, data);
		}

		public void CalcSHA1()
		{
			if (type != DiskItemType.File)
				return;

			var data = File.ReadAllBytes(FullName);
			SHA1 = Checksum.Get(Checksum.Type.SHA1, data);
		}

		public void Rename(string newName)
		{
			if (new DiskItem(newName).Exists)
				throw new Exception("A file or directory with that name already exists.");

			switch (type)
			{
				case DiskItemType.File: File.Move(FullName, newName); break;
				case DiskItemType.Directory: Directory.Move(FullName, newName); break;
				default: throw new Exception("Can only move file and directories.");
			}

			FullName = newName;
		}

		public void Relocate(string oldPath, string newPath)
		{
			oldPath += @"\";
			if (FullName.StartsWith(oldPath))
				FullName = newPath + @"\" + FullName.Substring(oldPath.Length);
		}

		string GetKey()
		{
			long ticks = 0;
			var time = WriteTime;
			if (time.HasValue)
				ticks = time.Value.Ticks;
			return String.Format("{0}-{1}-{2}-{3}", type, Name, Size, ticks);
		}

		public void Delete()
		{
			switch (type)
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
