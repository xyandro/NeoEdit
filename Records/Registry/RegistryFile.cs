using System;
using System.IO;

namespace NeoEdit.Records.Registry
{
	public class RegistryFile : IRecordItem
	{
		public IRecordList Parent { get { return new RegistryDir(Path.GetDirectoryName(FullName)); } }
		public string Name { get { return Path.GetFileName(FullName); } }
		public string FullName { get; private set; }
		public long Size { get; private set; }
		public byte[] Read(Int64 position, int bytes)
		{
			return new byte[0];
		}

		public RegistryFile(string key)
		{
			FullName = key;
		}
	}
}
