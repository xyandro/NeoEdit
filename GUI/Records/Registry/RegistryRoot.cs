using System.Collections.Generic;

namespace NeoEdit.GUI.Records.Registry
{
	public class RegistryRoot : RegistryRecord
	{
		public RegistryRoot() : base("Registry") { }

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
