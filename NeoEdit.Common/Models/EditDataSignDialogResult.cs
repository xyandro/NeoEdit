using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Models
{
	public class EditDataSignDialogResult
	{
		public Coder.CodePage CodePage { get; set; }
		public Cryptor.Type CryptorType { get; set; }
		public string Key { get; set; }
		public string Hash { get; set; }
	}
}
