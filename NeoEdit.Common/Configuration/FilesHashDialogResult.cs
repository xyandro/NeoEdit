using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Configuration
{
	public class FilesHashDialogResult : IConfiguration
	{
		public Hasher.Type HashType { get; set; }
		public byte[] HMACKey { get; set; }
	}
}
