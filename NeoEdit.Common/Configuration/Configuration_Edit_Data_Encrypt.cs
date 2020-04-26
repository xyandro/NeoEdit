using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Configuration
{
	public class Configuration_Edit_Data_Encrypt : IConfiguration
	{
		public Coder.CodePage InputCodePage { get; set; }
		public Cryptor.Type CryptorType { get; set; }
		public string Key { get; set; }
		public Coder.CodePage OutputCodePage { get; set; }
	}
}
