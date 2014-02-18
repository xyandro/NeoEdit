using System;

namespace NeoEdit.Common
{
	public delegate void IBinaryDataChangedDelegate();

	public interface IBinaryData
	{
		event IBinaryDataChangedDelegate Changed;
		byte[] GetAllBytes();
		byte[] GetSubset(long index, long count);
		long IndexOf(byte value, long start);
		long LastIndexOf(byte value, long start);
		long Length { get; }
		void Replace(long index, long count, byte[] bytes);
		byte this[long index] { get; }
	}
}
