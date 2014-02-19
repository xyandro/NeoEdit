namespace NeoEdit.Common
{
	public delegate void IBinaryDataChangedDelegate();

	public interface IBinaryData
	{
		event IBinaryDataChangedDelegate Changed;
		byte[] GetAllBytes();
		byte[] GetSubset(long index, long count);
		bool Find(FindData currentFind, long index, out long start, out long end, bool forward = true);
		long Length { get; }
		void Replace(long index, long count, byte[] bytes);
		byte this[long index] { get; }
	}
}
