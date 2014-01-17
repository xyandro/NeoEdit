namespace NeoEdit.Records
{
	public interface IRecordRoot : IRecordList
	{
		IRecord GetRecord(string uri);
	}
}
