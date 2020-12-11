using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Configuration
{
	public class Configuration_Edit_Advanced_Hash : IConfiguration
	{
		public Coder.CodePage CodePage { get; set; }
		public Hasher.Type HashType { get; set; }
	}
}
