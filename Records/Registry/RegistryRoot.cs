using System.Collections.Generic;

namespace NeoEdit.Records.Registry
{
	public class RegistryRoot : RecordRoot
	{
		public RegistryRoot(RecordList parent) : base("Registry", parent) { }

		public override Record GetRecord(string uri)
		{
			if (RegistryHelpers.MayBeRegKey(uri))
				return base.GetRecord(uri);
			return null;
		}

		public override IEnumerable<Record> Records
		{
			get
			{
				foreach (var key in RegistryHelpers.RootKeys.Keys)
					yield return new RegistryDir(key, this);
			}
		}
	}
}
