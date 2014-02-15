using System.Collections.Generic;

namespace NeoEdit.Records.Registry
{
	public class RegistryRoot : RegistryRecord
	{
		public RegistryRoot() : base("Registry") { }

		public override Record GetRecord(string uri)
		{
			if (RegistryRecord.MayBeRegKey(uri))
				return base.GetRecord(uri);
			return null;
		}

		public override IEnumerable<Record> Records
		{
			get
			{
				foreach (var key in RegistryRecord.RootKeys.Keys)
					yield return new RegistryDir(key);
			}
		}
	}
}
