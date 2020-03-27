using NeoEdit.Common.Transform;

namespace NeoEdit.Common
{
	public static class NoWindowDialogs
	{
		public delegate string RunCryptorKeyDialogDelegate(Cryptor.Type type, bool encrypt);
		public static RunCryptorKeyDialogDelegate RunCryptorKeyDialog { get; set; }
	}
}
