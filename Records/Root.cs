using System.Collections.Generic;

namespace NeoEdit.Records
{
	public class Root : RecordRoot
	{
		public static Root AllRoot { get; private set; }
		static Root() { AllRoot = new Root(null); }

		readonly List<Record> RootNodes;
		Root(Record parent)
			: base("Root", null)
		{
			RootNodes = new List<Record> { 
				new Disk.DiskRoot(this),
				new Network.NetworkRoot(this),
				new List.ListRoot(this),
				new Registry.RegistryRoot(this),
			};
		}

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
				foreach (var node in RootNodes)
					yield return node;
			}
		}
	}
}
