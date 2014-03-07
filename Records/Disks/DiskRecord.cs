using System;
using System.IO;
using System.Text.RegularExpressions;

namespace NeoEdit.Records.Disks
{
	public abstract class DiskRecord : Record
	{
		public DiskRecord(string uri) : base(uri) { }

		public override Type GetRootType() { return typeof(DiskRecord); }

		public override Record GetRecord(string uri)
		{
			uri = uri.Replace("/", @"\");
			uri = uri.Replace("\"", "");
			uri = uri.Trim();
			uri = uri.TrimEnd('\\');
			var netPath = uri.StartsWith(@"\\");
			uri = Regex.Replace(uri, @"\\+", @"\");
			if (netPath)
				uri = @"\" + uri;

			if (netPath)
			{
				var idx = uri.IndexOf('\\', 2);
				DiskRoot.EnsureShareExists(uri.Substring(0, idx == -1 ? uri.Length : idx));

				// Shares don't "exist" so don't throw them out
				if ((idx != -1) && (!File.Exists(uri)) && (!Directory.Exists(uri)))
					return null;
			}
			else
			{
				if ((!File.Exists(uri)) && (!Directory.Exists(uri)))
					return null;
			}

			return base.GetRecord(uri);
		}

		public override Record Parent
		{
			get
			{
				if (this is DiskRoot)
					return new Root();

				if (this is NetworkShare)
					return new DiskRoot();

				var parent = GetProperty<string>(RecordProperty.PropertyName.Path);
				if ((parent.StartsWith(@"\\")) && (parent.IndexOf('\\', 2) == -1))
					return new NetworkShare(parent);

				if (parent.Length == 0)
					return new DiskRoot();

				return new DiskDir(parent);
			}
		}
	}
}
