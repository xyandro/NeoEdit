using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Records.Registry
{
	public class RegistryRoot : IRecordList
	{
		public static Dictionary<string, RegistryKey> RootKeys { get; private set; }
		static RegistryRoot()
		{
			var list = new List<RegistryKey> { 
				Microsoft.Win32.Registry.ClassesRoot,
				Microsoft.Win32.Registry.CurrentUser,
				Microsoft.Win32.Registry.LocalMachine,
				Microsoft.Win32.Registry.Users,
				Microsoft.Win32.Registry.CurrentConfig,
				Microsoft.Win32.Registry.PerformanceData,
			};
			RootKeys = list.ToDictionary(a => a.Name, a => a);
		}

		static Func<String, IRecordList> Provider { get { return name => name.Equals(RootName, StringComparison.OrdinalIgnoreCase) ? new RegistryRoot() : null; } }
		public static string RootName { get { return "Registry"; } }

		public IRecordList Parent { get { return new RootRecordList(); } }
		public string Name { get { return RootName; } }
		public string FullName { get { return RootName; } }
		public IEnumerable<IRecord> Records
		{
			get
			{
				foreach (var key in RootKeys)
					yield return new RegistryDir(key.Key);
			}
		}
	}
}
