using System.Collections.Generic;
using System.IO;

namespace NeoEdit.SevenZip
{
	class ArchiveExtractAllCallback : IArchiveExtractCallback
	{
		public bool Success { get; private set; }
		List<SevenZipEntry> entries;
		string path;
		OutStreamWrapper lastStream;

		public ArchiveExtractAllCallback(List<SevenZipEntry> entries, string path)
		{
			this.entries = entries;
			this.path = path;
			Success = true;
		}

		public void SetTotal(ulong total) { }
		public void SetCompleted(ref ulong completeValue) { }
		public void PrepareOperation(AskMode askExtractMode) { }

		public int GetStream(int index, out ISequentialOutStream outStream, AskMode askExtractMode)
		{
			var entry = entries[index];
			outStream = null;
			var outPath = Path.Combine(path, entry.Path);
			if (entry.IsDirectory)
				Directory.CreateDirectory(outPath);
			else
				outStream = lastStream = new OutStreamWrapper(File.Create(outPath));
			return 0;
		}

		public void SetOperationResult(OperationResult result)
		{
			lastStream?.Dispose();
			lastStream = null;
			if (result != OperationResult.Ok)
				Success = false;
		}
	}
}
