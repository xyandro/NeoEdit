using System;
using System.IO;

namespace NeoEdit.Disk
{
	public class FilePath : IDisposable
	{
		public readonly string Path;
		public readonly Stream Stream;

		public FilePath(string path, Stream stream = null)
		{
			Path = path;
			Stream = stream;
		}

		public void Dispose()
		{
			if (Stream != null)
				Stream.Dispose();
		}

		public override string ToString() => Path;
	}
}
