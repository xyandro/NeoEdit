using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Configuration
{
	public class FilesSignDialogResult : IConfiguration
	{
		public Cryptor.Type CryptorType { get; set; }
		public string Key { get; set; }
		public string Hash { get; set; }
	}
}
