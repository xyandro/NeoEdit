using System.Collections.Generic;

namespace NeoEdit.Records.Registry
{
	public class RegistryRoot : IRecordRoot
	{
		public IRecord GetRecord(string uri)
		{
			if ((uri == FullName) || (RegistryHelpers.MayBeRegKey(uri)))
				return RecordListProvider.GetRecord(uri, this);
			return null;
		}

		public IRecordList Parent { get { return new RootRecordList(); } }
		public string Name { get { return Name; } }
		public string FullName { get { return "Registry"; } }
		public IEnumerable<IRecord> Records
		{
			get
			{
				foreach (var key in RegistryHelpers.RootKeys.Keys)
					yield return new RegistryDir(key);
			}
		}
	}
}
