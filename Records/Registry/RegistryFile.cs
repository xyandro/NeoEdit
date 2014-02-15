using System;
using System.IO;

namespace NeoEdit.Records.Registry
{
	public class RegistryFile : RegistryRecord
	{
		public RegistryFile(string uri)
			: base(uri)
		{
			if (String.IsNullOrEmpty(Name))
				this[RecordProperty.PropertyName.Name] = "(Default)";
			using (var key = GetKey(Path.GetDirectoryName(FullName)))
			{
				this[RecordProperty.PropertyName.Type] = key.GetValueKind(Path.GetFileName(FullName)).ToString();
				var value = key.GetValue(Path.GetFileName(FullName));
				if (value is byte[])
					value = BitConverter.ToString(value as byte[]).Replace("-", " ").ToLower();
				else if (value is string[])
					value = string.Join(" ", value as string[]);
				else if (value is int)
					value = String.Format("0x{0:x8} / {1} / {2}", value, value, (uint)(int)value);
				else if (value is long)
					value = String.Format("0x{0:x16} / {1} / {2}", value, value, (ulong)(long)value);
				this[RecordProperty.PropertyName.Data] = value.ToString();
			}
		}

		public override bool IsFile { get { return true; } }
	}
}
