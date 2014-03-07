using System.Collections.Generic;

namespace NeoEdit.Records.Registries
{
	public class RegistryDir : RegistryRecord
	{
		public RegistryDir(string uri) : base(uri) { }

		public override IEnumerable<Record> Records
		{
			get
			{
				using (var subKey = GetKey(FullName))
				{
					foreach (var name in subKey.GetSubKeyNames())
						yield return new RegistryDir(FullName + @"\" + name);
					foreach (var name in subKey.GetValueNames())
						yield return new RegistryFile(FullName + @"\" + name);
				}
			}
		}
	}
}
