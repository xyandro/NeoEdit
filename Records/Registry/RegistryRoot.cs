using System;
using System.Collections.Generic;

namespace NeoEdit.Records.Registry
{
	public class RegistryRoot : RecordRoot
	{
		public RegistryRoot(Record parent) : base("Registry", parent) { }

		public override Record GetRecord(string uri)
		{
			if (RegistryRecord.MayBeRegKey(uri))
				return base.GetRecord(uri);
			return null;
		}

		protected override IEnumerable<Tuple<string, Func<string, Record>>> InternalRecords
		{
			get
			{
				foreach (var key in RegistryRecord.RootKeys.Keys)
					yield return new Tuple<string, Func<string, Record>>(key, a => new RegistryDir(a, this));
			}
		}
	}
}
