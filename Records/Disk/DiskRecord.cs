namespace NeoEdit.Records.Disk
{
	public abstract class DiskRecord : Record
	{
		public DiskRecord(string uri, Record parent) : base(uri, parent) { }
	}
}
