using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace NeoEdit.Records.Registry
{
	public class RegistryDir : RecordList
	{
		public RegistryDir(string uri) : base(uri) { }

		public override RecordList Parent
		{
			get
			{
				var parent = Path.GetDirectoryName(FullName);
				if (String.IsNullOrEmpty(parent))
					return new RegistryRoot();
				return new RegistryDir(parent);
			}
		}

		public override IEnumerable<Record> Records
		{
			get
			{
				using (var subKey = RegistryHelpers.GetKey(FullName))
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
