using System.Collections.Generic;

namespace NeoEdit.Records.Registry
{
	public class RegistryDir : RecordList
	{
		public RegistryDir(string uri, RecordList parent) : base(uri, parent) { }

		protected override IEnumerable<Record> InternalRecords
		{
			get
			{
				using (var subKey = RegistryHelpers.GetKey(FullName))
				{
					foreach (var name in subKey.GetSubKeyNames())
						yield return new RegistryDir(FullName + @"\" + name, this);
					foreach (var name in subKey.GetValueNames())
						yield return new RegistryFile(FullName + @"\" + name, this);
				}
			}
		}
	}
}
