namespace NeoEdit.Records
{
	public interface IRecord
	{
		IRecordList Parent { get; }
		string FullName { get; }
		string Name { get; }
	}
}
