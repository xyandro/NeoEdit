using System.Collections.Generic;

namespace NeoEdit.Records
{
	public class RootRecordList : RecordRoot
	{
		public RootRecordList() : base("Root") { }
		public override Record GetRecord(string uri) { return null; }
		public override RecordList Parent { get { return this; } }
		public override IEnumerable<Record> Records
		{
			get
			{
				yield return new Disk.DiskRoot();
				yield return new Registry.RegistryRoot();
			}
		}
	}
}
