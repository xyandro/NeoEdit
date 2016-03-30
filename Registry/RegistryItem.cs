using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using NeoEdit.Common;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Controls.ItemGridControl;

namespace NeoEdit.Registry
{
	public class RegistryItem : ItemGridTreeItem
	{
		[DepProp]
		public string Name { get { return UIHelper<RegistryItem>.GetPropValue<string>(this); } private set { UIHelper<RegistryItem>.SetPropValue(this, value); } }
		[DepProp]
		public string Type { get { return UIHelper<RegistryItem>.GetPropValue<string>(this); } private set { UIHelper<RegistryItem>.SetPropValue(this, value); } }
		[DepProp]
		public string Data { get { return UIHelper<RegistryItem>.GetPropValue<string>(this); } private set { UIHelper<RegistryItem>.SetPropValue(this, value); } }

		readonly bool isSubKey;

		static RegistryItem() { UIHelper<RegistryItem>.Register(); }

		public RegistryItem() : this(null, null, true) { }
		RegistryItem(RegistryKey key, string name, bool isSubKey)
			: base((key == null ? "" : key.Name + @"\") + name)
		{
			Name = string.IsNullOrEmpty(name) ? "(Default)" : name;
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
					value = $"0x{value:x8} / {value} / {(uint)(int)value}";
				else if (value is long)
					value = $"0x{value:x16} / {value} / {(ulong)(long)value}";

				Type = key.GetValueKind(name).ToString();
				Data = value.ToString();
			}
		}

		public static string Simplify(string location) => Regex.Replace(location.Trim().Trim('"'), @"[\\/]+", @"\");

		public override ItemGridTreeItem GetParent()
		{
			if (FullName == "")
				return null;

			var idx = FullName.LastIndexOf('\\');
			var fullName = idx == -1 ? "" : FullName.Substring(0, idx);
			return new RegistryItem().GetChild(fullName);
		}

		public override bool CanGetChildren() => isSubKey;

		static Dictionary<string, RegistryKey> RootKeys = Helpers.GetValues<RegistryHive>().Select(a => RegistryKey.OpenBaseKey(a, RegistryView.Registry64)).ToDictionary(value => value.Name, value => value);
		public override IEnumerable<ItemGridTreeItem> GetChildren()
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
						throw new ArgumentException($"Invalid key: {FullName}");
				}

				foreach (var child in key.GetSubKeyNames())
					yield return new RegistryItem(key, child, true);
				foreach (var child in key.GetValueNames())
					yield return new RegistryItem(key, child, false);
			}
		}

		public override string ToString() => FullName;
	}
}
