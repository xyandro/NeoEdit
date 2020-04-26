using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Configuration
{
	public class Configuration_Files_Hash : IConfiguration
	{
		public Hasher.Type HashType { get; set; }
		public byte[] HMACKey { get; set; }
	}
}
