using System;
using System.Collections.Generic;

namespace NeoEdit.Records.Registry
{
	public class RegistryDir : Record
	{
		public RegistryDir(string uri, Record parent) : base(uri, parent) { }

		protected override IEnumerable<Tuple<string, Func<string, Record>>> InternalRecords
		{
			get
			{
				using (var subKey = RegistryHelpers.GetKey(FullName))
				{
					foreach (var name in subKey.GetSubKeyNames())
						yield return new Tuple<string, Func<string, Record>>(FullName + @"\" + name, a => new RegistryDir(a, this));
					foreach (var name in subKey.GetValueNames())
						yield return new Tuple<string, Func<string, Record>>(FullName + @"\" + name, a => new RegistryFile(a, this));
				}
			}
		}
	}
}
