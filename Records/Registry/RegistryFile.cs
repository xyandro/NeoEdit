using System.IO;

namespace NeoEdit.Records.Registry
{
	public class RegistryFile : RecordItem
	{
		public RegistryFile(string uri) : base(uri) { }
		public override RecordList Parent { get { return new RegistryDir(Path.GetDirectoryName(FullName)); } }
	}
}
