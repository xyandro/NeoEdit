using System;
using System.IO;

namespace NeoEdit.Disk
{
	public class FilePath : IDisposable
	{
		public string Path { get; private set; }
		bool isTmp = false;
		Stream stream;

		public FilePath(string path)
		{
			Path = path;
		}

		public FilePath(Stream input)
		{
			Path = System.IO.Path.GetTempPath() + "NeoEdit-" + Guid.NewGuid().ToString() + ".tmp";
			isTmp = true;
			using (stream = File.OpenWrite(Path))
				input.CopyTo(stream);
		}

		public void Dispose()
		{
			if (isTmp)
				File.Delete(Path);
		}

		public override string ToString() { return Path; }
	}
}
