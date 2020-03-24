using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Models
{
	public class EditDataEncryptDialogResult
	{
		public Coder.CodePage InputCodePage { get; set; }
		public Cryptor.Type CryptorType { get; set; }
		public string Key { get; set; }
		public Coder.CodePage OutputCodePage { get; set; }
	}
}
