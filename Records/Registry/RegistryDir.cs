using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NeoEdit.Records.Registry
{
	public class RegistryDir : IRecordList
	{
		static Regex registryRE = new Regex(String.Format("^({0})(?:\\\\(.*))?$", String.Join("|", RegistryRoot.RootKeys.Select(a => a.Key))), RegexOptions.IgnoreCase);

		static string FixCasing(string key)
		{
			var ret = key;
			var parts = ret.Split('\\').ToList();
			ret = RegistryRoot.RootKeys.Select(a => a.Key).Single(a => a.Equals(parts[0], StringComparison.OrdinalIgnoreCase));
			parts.RemoveAt(0);

			foreach (var part in parts)
				using (var subKey = GetKey(ret))
				{
					var next = subKey.GetSubKeyNames().SingleOrDefault(a => a.Equals(Path.GetFileName(part), StringComparison.OrdinalIgnoreCase));
					if (next == null)
						next = subKey.GetValueNames().SingleOrDefault(a => a.Equals(Path.GetFileName(part), StringComparison.OrdinalIgnoreCase));
					if (next == null)
						throw new Exception(String.Format("Invalid registry key: {0}", key));
					ret += @"\" + next;
				}

			return ret;
		}

		static bool IsValue(string key)
		{
			using (var subKey = GetKey(Path.GetDirectoryName(key)))
				return subKey.GetValueNames().Any(a => a == Path.GetFileName(key));
		}

		static Func<String, IRecordList> Provider
		{
			get
			{
				return name =>
				{
					name = name.Replace("/", "\\");
					while (true)
					{
						var oldName = name;
						name = name.Replace("\\\\", "\\");
						name = name.Trim().TrimEnd('\\');
						if ((name.StartsWith("\"")) && (name.EndsWith("\"")))
							name = name.Substring(1, name.Length - 2);
						if (oldName == name)
							break;
					}
					if (!registryRE.IsMatch(name))
						return null;

					try { name = FixCasing(name); }
					catch { return null; }

					if (IsValue(name))
						name = Path.GetDirectoryName(name);

					return new RegistryDir(name);
				};
			}
		}

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
			return GetKey(FullName);
		}

		static RegistryKey GetKey(string key)
		{
			var match = registryRE.Match(key);
			if (!match.Success)
				throw new Exception(String.Format("Invalid registry key: {0}", key));

			var subKey = RegistryRoot.RootKeys[match.Groups[1].Value];
			key = match.Groups[2].Success ? match.Groups[2].Value : null;
			if (key != null)
				subKey = subKey.OpenSubKey(key);
			return subKey;
		}

		public RegistryDir(string key)
		{
			FullName = key;
			Name = Path.GetFileName(key);
		}
	}
}
