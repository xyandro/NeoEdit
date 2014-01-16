using System;
using System.Collections.Generic;

namespace NeoEdit.Records
{
	public class RootRecordList : IRecordList
	{
		static Func<String, IRecordList> Provider { get { return name => name.Equals("Root", StringComparison.OrdinalIgnoreCase) ? new RootRecordList() : null; } }

		public IRecordList Parent { get { return this; } }
		public string Name { get { return "Root"; } }
		public string FullName { get { return "Root"; } }
		public IEnumerable<IRecord> Records
		{
			get
			{
				yield return new Disk.DiskRoot();
				yield return new Registry.RegistryRoot();
			}
		}
	}
}
