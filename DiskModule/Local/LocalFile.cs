using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeoEdit.DiskModule;

namespace NeoEdit.DiskModule.Local
{
	public class LocalFile : IDisposable, IFile
	{
		public string fileName { get; private set; }
		FileStream file;
		public long length { get; private set; }
		public LocalFile(string _filename)
		{
			fileName = _filename;
			file = File.OpenRead(fileName);
			length = file.Length;
		}

		public byte[] Read(long position, int bytes)
		{
			if (position >= length)
				return new byte[0];
			var data = new byte[Math.Min(length - position, bytes)];
			file.Position = position;
			file.Read(data, 0, data.Length);
			return data;
		}

		public void Dispose()
		{
			file.Close();
		}
	}
}
