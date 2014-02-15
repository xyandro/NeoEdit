using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NeoEdit.Records.Disk
{
	public abstract class DiskRecord : Record
	{
		public DiskRecord(string uri) : base(uri) { }

		public override Type GetRootType() { return typeof(DiskRecord); }

		public override Record Parent
		{
			get
			{
				if (this is DiskRoot)
					return new Root();

				var parent = Path.GetDirectoryName(FullName);
				if (parent == null)
				{
					if (FullName.StartsWith(@"\\"))
					{
						var idx = FullName.IndexOf('\\', 2);
						if (idx != -1)
							return new Network.NetworkDir(FullName.Substring(0, idx));
					}

					return new DiskRoot();
				}

				if ((!parent.StartsWith(@"\\")) && (Path.GetDirectoryName(parent) == null))
					parent = parent.Substring(0, 2);

				return new DiskDir(parent);
			}
		}

		public override IEnumerable<RecordAction.ActionName> Actions
		{
			get
			{
				return new List<RecordAction.ActionName> { 
					RecordAction.ActionName.Rename,
					RecordAction.ActionName.Delete,
					RecordAction.ActionName.Copy,
					RecordAction.ActionName.Cut,
					RecordAction.ActionName.SyncSource,
					RecordAction.ActionName.SyncTarget,
				}.Concat(base.Actions);
			}
		}

		public override void Rename(string newName, System.Func<bool> canOverwrite)
		{
			newName = Path.Combine(Path.GetDirectoryName(FullName), newName);
			if (Directory.Exists(newName))
				throw new Exception("A directory already exists at the specified location.");

			if (File.Exists(newName))
			{
				if (!canOverwrite())
					return;

				File.Delete(newName);
			}

			if (this is DiskDir)
				Directory.Move(FullName, newName);
			else if (this is DiskFile)
				File.Move(FullName, newName);
			FullName = newName;
		}
	}
}
