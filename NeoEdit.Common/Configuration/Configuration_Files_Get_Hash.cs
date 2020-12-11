using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Configuration
{
	public class Configuration_Files_Get_Hash : IConfiguration
	{
		public Hasher.Type HashType { get; set; }
	}
}
