using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.Win32;
using NeoEdit.Common;

namespace NeoEdit.Registry
{
	class MyDepPropAttribute : Attribute { }

	public class RegistryItem : DependencyObject
	{
		static Dictionary<string, DependencyProperty> dependencyProperty;
		static RegistryItem()
		{
			var properties = typeof(RegistryItem).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(itr1 => itr1.CustomAttributes.Any(itr2 => itr2.AttributeType == typeof(MyDepPropAttribute))).ToList();
			dependencyProperty = properties.ToDictionary(itr => itr.Name, itr => DependencyProperty.Register(itr.Name, itr.PropertyType, typeof(RegistryItem)));
		}

		public static IEnumerable<DependencyProperty> GetDepProps()
		{
			return dependencyProperty.Values;
		}

		T GetValue<T>([CallerMemberName] string caller = "") { return (T)GetValue(dependencyProperty[caller]); }
		void SetValue<T>(T value, [CallerMemberName] string caller = "") { SetValue(dependencyProperty[caller], value); }

		[MyDepProp]
		public string Name { get { return GetValue<string>(); } private set { SetValue(value); } }
		[MyDepProp]
		public string Type { get { return GetValue<string>(); } private set { SetValue(value); } }
		[MyDepProp]
		public string Data { get { return GetValue<string>(); } private set { SetValue(value); } }
		[MyDepProp]
		public string FullName { get { return GetValue<string>(); } private set { SetValue(value); } }
		public bool IsKey { get { return Type == "Key"; } set { if (value)Type = "Key"; } }

		RegistryItem() : this("", "") { }
		RegistryItem(string parent, string name)
		{
			FullName = String.IsNullOrEmpty(parent) ? name : parent + @"\" + name;
			Name = String.IsNullOrEmpty(name) ? "(Default)" : name;
		}

		public static string GetProperKey(string key)
		{
			var parts = key.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries).ToList();

			string result = "";
			foreach (var part in parts)
			{
				var keys = GetKeys(result);
				var item = keys.FirstOrDefault(a => a.Name.Equals(part, StringComparison.InvariantCultureIgnoreCase));
				if (item == null)
					return null;
				result = item.FullName;
			}

			return result;
		}

		public static string GetParent(string key)
		{
			var idx = key.LastIndexOf('\\');
			if (idx == -1)
				return "";
			return key.Substring(0, idx);
		}

		static Dictionary<string, RegistryKey> RootKeys = Helpers.GetValues<RegistryHive>().Select(a => RegistryKey.OpenBaseKey(a, RegistryView.Registry64)).ToDictionary(value => value.Name, value => value);
		public static List<RegistryItem> GetKeys(string parent)
		{
			if (parent.Length == 0)
				return RootKeys.Select(itr => new RegistryItem(parent, itr.Key) { Type = "Key" }).ToList();

			var idx = parent.IndexOf('\\');
			var root = idx == -1 ? parent : parent.Substring(0, idx);
			if (!RootKeys.ContainsKey(root))
				throw new ArgumentException("Invalid key: " + parent);

			var key = RootKeys[root];
			if (idx != -1)
			{
				key = key.OpenSubKey(parent.Substring(idx + 1));
				if (key == null)
					throw new ArgumentException("Invalid key: " + parent);
			}

			var result = key.GetSubKeyNames().Select(itr => new RegistryItem(parent, itr) { Type = "Key" }).ToList();
			foreach (var value in key.GetValueNames())
			{
				var myValue = key.GetValue(value);
				if (myValue is byte[])
					myValue = BitConverter.ToString(myValue as byte[]).Replace("-", " ").ToLower();
				else if (myValue is string[])
					myValue = string.Join(" ", myValue as string[]);
				else if (myValue is int)
					myValue = String.Format("0x{0:x8} / {1} / {2}", myValue, myValue, (uint)(int)myValue);
				else if (myValue is long)
					myValue = String.Format("0x{0:x16} / {1} / {2}", myValue, myValue, (ulong)(long)myValue);

				result.Add(new RegistryItem(parent, value) { Type = key.GetValueKind(value).ToString(), Data = myValue.ToString() });
			}

			return result;
		}

		public static void UpdateKeys(string keyStr, ObservableCollection<RegistryItem> keys)
		{
			keys.Clear();
			foreach (var key in GetKeys(keyStr))
				keys.Add(key);
		}
	}
}
