namespace NeoEdit.Common
{
	public class SyncParams
	{
		public enum SyncType
		{
			NameSizeLength,
		}

		public SyncParams()
		{
			StopOnError = true;
		}

		public SyncType Type { get; set; }
		public bool EraseExtra { get; set; }
		public bool StopOnError { get; set; }
		public bool LogOnly { get; set; }
	}
}
