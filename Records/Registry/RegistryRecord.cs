using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace NeoEdit.GUI.Records.Registry
{
	public abstract class RegistryRecord : Record
	{
		static Regex RegistryRE;
		public static Dictionary<string, RegistryKey> RootKeys { get; private set; }

		public override Type GetRootType()
		{
			return typeof(RegistryRecord);
		}

		public override Record GetRecord(string uri)
		{
			if (RegistryRE.IsMatch(uri))
				return base.GetRecord(uri);
			return null;
		}

		public override Record Parent
		{
			get
			{
				if (this is RegistryRoot)
					return new Root();

				var parent = GetProperty<string>(RecordProperty.PropertyName.Path);
				if (String.IsNullOrEmpty(parent))
					return new RegistryRoot();
				return new RegistryDir(parent);
			}
		}

		static RegistryRecord()
		{
			var list = Helpers.GetValues<RegistryHive>().Select(a => RegistryKey.OpenBaseKey(a, RegistryView.Registry64)).ToList();
			RootKeys = list.ToDictionary(a => a.Name, a => a);
			RegistryRE = new Regex(String.Format("^({0})(?:\\\\(.*))?$", String.Join("|", RootKeys.Select(a => a.Key))), RegexOptions.IgnoreCase);
		}

		public RegistryRecord(string uri) : base(uri) { }

		protected RegistryKey GetKey(string key)
		{
			var match = RegistryRE.Match(key);
			if (!match.Success)
				throw new Exception(String.Format("Invalid registry key: {0}", key));

			var subKey = RootKeys[match.Groups[1].Value];
			key = match.Groups[2].Success ? match.Groups[2].Value : null;
			if (key != null)
				subKey = subKey.OpenSubKey(key);
			return subKey;
		}
	}
}
