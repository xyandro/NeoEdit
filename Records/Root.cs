using System.Collections.Generic;

namespace NeoEdit.Records
{
	public class Root : RecordRoot
	{
		public Root() : base("Root") { }

		public override Record GetRecord(string uri)
		{
			foreach (var record in Records)
			{
				var root = record as RecordRoot;
				if (root != null)
				{
					try
					{
						var tempRecord = root.GetRecord(uri);
						if (tempRecord != null)
							return tempRecord;
					}
					catch { }
				}
			}
			return null;
		}

		public override IEnumerable<Record> Records
		{
			get
			{
				yield return new Disk.DiskRoot();
				yield return new Network.NetworkRoot();
				yield return new List.ListRoot();
				yield return new Registry.RegistryRoot();
			}
		}
	}
}
