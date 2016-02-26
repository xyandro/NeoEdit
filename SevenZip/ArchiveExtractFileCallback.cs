using System.IO;

namespace NeoEdit.SevenZip
{
	class ArchiveExtractFileCallback : IArchiveExtractCallback
	{
		public bool Success { get; private set; }
		uint index;
		Stream stream;

		public ArchiveExtractFileCallback(uint index, Stream stream)
		{
			this.index = index;
			this.stream = stream;
		}

		public void SetTotal(ulong total) { }
		public void SetCompleted(ref ulong completeValue) { }
		public void PrepareOperation(AskMode askExtractMode) { }

		public int GetStream(int index, out ISequentialOutStream outStream, AskMode askExtractMode)
		{
			outStream = null;
			if ((index == this.index) && (askExtractMode == AskMode.kExtract))
				outStream = new OutStreamWrapper(stream);

			return 0;
		}

		public void SetOperationResult(OperationResult result) => Success = result == OperationResult.Ok;
	}
}
