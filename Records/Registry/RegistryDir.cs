using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace NeoEdit.Records.Registry
{
	public class RegistryDir : IRecordList
	{
		public IRecordList Parent
		{
			get
			{
				var parent = Path.GetDirectoryName(FullName);
				if (String.IsNullOrEmpty(parent))
					return new RegistryRoot();
				return new RegistryDir(parent);
			}
		}
		public string Name { get; private set; }
		public string FullName { get; private set; }
		public IEnumerable<IRecord> Records
		{
			get
			{
				using (var subKey = GetKey())
				{
					foreach (var name in subKey.GetSubKeyNames())
						yield return new RegistryDir(FullName + @"\" + name);
					foreach (var name in subKey.GetValueNames())
						yield return new RegistryFile(FullName + @"\" + name);
				}
			}
		}

		RegistryKey GetKey()
		{
			return RegistryHelpers.GetKey(FullName);
		}

		public RegistryDir(string key)
		{
			FullName = key;
			Name = Path.GetFileName(key);
		}
	}
}
