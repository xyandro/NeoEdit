using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Configuration
{
	public class EditDataSignDialogResult : IConfiguration
	{
		public Coder.CodePage CodePage { get; set; }
		public Cryptor.Type CryptorType { get; set; }
		public string Key { get; set; }
		public string Hash { get; set; }
	}
}
