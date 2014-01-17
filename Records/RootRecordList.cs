using System.Collections.Generic;

namespace NeoEdit.Records
{
	public class RootRecordList : RecordRoot
	{
		public static RootRecordList RootRecord { get; private set; }
		static RootRecordList()
		{
			RootRecord = new RootRecordList(null);
		}

		RootRecordList(RecordList parent) : base("Root", null) { }
		public override Record GetRecord(string uri) { return null; }
		public override IEnumerable<Record> Records
		{
			get
			{
				yield return new Disk.DiskRoot(this);
				yield return new Registry.RegistryRoot(this);
			}
		}
	}
}
