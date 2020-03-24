using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Models
{
	public class FilesSignDialogResult
	{
		public Cryptor.Type CryptorType { get; set; }
		public string Key { get; set; }
		public string Hash { get; set; }
	}
}
