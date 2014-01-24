using System;
using System.IO;

namespace NeoEdit.Records.Registry
{
	public class RegistryFile : Record
	{
		public RegistryFile(string uri, Record parent)
			: base(uri, parent)
		{
			if (String.IsNullOrEmpty(Name))
				this[Property.PropertyType.Name] = "(Default)";
			using (var key = RegistryHelpers.GetKey(Path.GetDirectoryName(FullName)))
			{
				this[Property.PropertyType.Type] = key.GetValueKind(Path.GetFileName(FullName));
				this[Property.PropertyType.Data] = key.GetValue(Path.GetFileName(FullName));
			}
		}

		public override bool IsFile { get { return true; } }
	}
}
