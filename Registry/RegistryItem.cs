using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using NeoEdit.Common;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.ItemGridControl;

namespace NeoEdit.Registry
{
	public class RegistryItem : ItemGridTreeItem<RegistryItem>
	{
		[DepProp]
		public string Name { get { return GetValue<string>(); } private set { SetValue(value); } }
		[DepProp]
		public string Type { get { return GetValue<string>(); } private set { SetValue(value); } }
		[DepProp]
		public string Data { get { return GetValue<string>(); } private set { SetValue(value); } }

		readonly bool isSubKey;

		public RegistryItem() : this(null, null, true) { }
		RegistryItem(RegistryKey key, string name, bool isSubKey)
			: base((key == null ? "" : key.Name + @"\") + name)
		{
			Name = String.IsNullOrEmpty(name) ? "(Default)" : name;
			this.isSubKey = isSubKey;

			if (isSubKey)
				Type = "Key";
			else
			{
				var value = key.GetValue(name);
				if (value is byte[])
					value = BitConverter.ToString(value as byte[]).Replace("-", " ").ToLower();
				else if (value is string[])
					value = string.Join(" ", value as string[]);
				else if (value is int)
					value = String.Format("0x{0:x8} / {1} / {2}", value, value, (uint)(int)value);
				else if (value is long)
					value = String.Format("0x{0:x16} / {1} / {2}", value, value, (ulong)(long)value);

				Type = key.GetValueKind(name).ToString();
				Data = value.ToString();
			}
		}

		public override IItemGridTreeItem GetParent()
		{
			if (FullName == "")
				return null;

			var idx = FullName.LastIndexOf('\\');
			var fullName = idx == -1 ? "" : FullName.Substring(0, idx);
			return new RegistryItem().GetChild(fullName);
		}

		public override bool CanGetChildren()
		{
			return isSubKey;
		}

		static Dictionary<string, RegistryKey> RootKeys = Helpers.GetValues<RegistryHive>().Select(a => RegistryKey.OpenBaseKey(a, RegistryView.Registry64)).ToDictionary(value => value.Name, value => value);
		public override IEnumerable<IItemGridTreeItem> GetChildren()
		{
			if (FullName == "")
			{
				foreach (var key in RootKeys)
					yield return new RegistryItem(null, key.Key, true);
			}
			else
			{
				var idx = FullName.IndexOf('\\');
				var root = idx == -1 ? FullName : FullName.Substring(0, idx);
				var key = RootKeys[root];
				if (idx != -1)
				{
					key = key.OpenSubKey(FullName.Substring(idx + 1));
					if (key == null)
						throw new ArgumentException("Invalid key: " + FullName);
				}

				foreach (var child in key.GetSubKeyNames())
					yield return new RegistryItem(key, child, true);
				foreach (var child in key.GetValueNames())
					yield return new RegistryItem(key, child, false);
			}
		}

		public override string ToString()
		{
			return FullName;
		}
	}
}
