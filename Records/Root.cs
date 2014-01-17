using System.Collections.Generic;

namespace NeoEdit.Records
{
	public class Root : RecordRoot
	{
		public static Root AllRoot { get; private set; }
		static Root() { AllRoot = new Root(null); }

		Root(RecordList parent) : base("Root", null) { }

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

		protected override IEnumerable<Record> InternalRecords
		{
			get
			{
				yield return new Disk.DiskRoot(this);
				yield return new Network.NetworkRoot(this);
				yield return new Registry.RegistryRoot(this);
			}
		}
	}
}
