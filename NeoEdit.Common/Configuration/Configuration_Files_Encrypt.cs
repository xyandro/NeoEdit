using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Configuration
{
	public class Configuration_Files_Encrypt : IConfiguration
	{
		public Cryptor.Type CryptorType { get; set; }
		public string Key { get; set; }
	}
}
