using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Configuration
{
	public class EditDataHashDialogResult : IConfiguration
	{
		public Coder.CodePage CodePage { get; set; }
		public Hasher.Type HashType { get; set; }
		public byte[] HMACKey { get; set; }
	}
}
