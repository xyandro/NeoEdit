using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Configuration
{
	public class Configuration_Numeric_MinMaxValues : IConfiguration
	{
		public Coder.CodePage CodePage { get; set; }
		public bool Min { get; set; }
		public bool Max { get; set; }
	}
}
