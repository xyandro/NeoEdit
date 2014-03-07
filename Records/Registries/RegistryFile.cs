using System;

namespace NeoEdit.Records.Registries
{
	public class RegistryFile : RegistryRecord
	{
		const string defaultStr = "(Default)";
		public RegistryFile(string uri)
			: base(uri)
		{
			if (String.IsNullOrEmpty(Name))
				this[RecordProperty.PropertyName.Name] = defaultStr;
			using (var key = GetKey(GetProperty<string>(RecordProperty.PropertyName.Path)))
			{
				var name = Name;
				if (name == defaultStr)
					name = "";
				this[RecordProperty.PropertyName.Type] = key.GetValueKind(name).ToString();
				var value = key.GetValue(name);
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
