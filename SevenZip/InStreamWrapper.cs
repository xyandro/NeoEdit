using System.IO;

namespace NeoEdit.SevenZip
{
	class InStreamWrapper : StreamWrapper, ISequentialInStream, IInStream
	{
		public InStreamWrapper(Stream baseStream) : base(baseStream) { }

		public int Read(byte[] data, int size) => baseStream.Read(data, 0, size);
	}
}
