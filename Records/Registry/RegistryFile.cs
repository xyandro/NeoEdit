using System;
using System.IO;

namespace NeoEdit.Records.Registry
{
	public class RegistryFile : RegistryRecord
	{
		public RegistryFile(string uri, Record parent)
			: base(uri, parent)
		{
			if (String.IsNullOrEmpty(Name))
				this[RecordProperty.PropertyName.Name] = "(Default)";
			using (var key = GetKey(Path.GetDirectoryName(FullName)))
			{
				this[RecordProperty.PropertyName.Type] = key.GetValueKind(Path.GetFileName(FullName)).ToString();
				this[RecordProperty.PropertyName.Data] = key.GetValue(Path.GetFileName(FullName));
			}
		}

		public override bool IsFile { get { return true; } }
	}
}
