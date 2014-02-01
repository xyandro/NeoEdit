﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NeoEdit.Records.Registry
{
	public abstract class RegistryRecord : Record
	{
		static Regex RegistryRE;
		public static Dictionary<string, Microsoft.Win32.RegistryKey> RootKeys { get; private set; }

		static RegistryRecord()
		{
			var list = Helpers.GetValues<Microsoft.Win32.RegistryHive>().Select(a => Microsoft.Win32.RegistryKey.OpenBaseKey(a, Microsoft.Win32.RegistryView.Registry64)).ToList();
			RootKeys = list.ToDictionary(a => a.Name, a => a);
			RegistryRE = new Regex(String.Format("^({0})(?:\\\\(.*))?$", String.Join("|", RootKeys.Select(a => a.Key))), RegexOptions.IgnoreCase);
		}

		public RegistryRecord(string uri, Record parent) : base(uri, parent) { }

		public static bool MayBeRegKey(string uri)
		{
			return RegistryRE.IsMatch(uri);
		}

		public Microsoft.Win32.RegistryKey GetKey(string key)
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
