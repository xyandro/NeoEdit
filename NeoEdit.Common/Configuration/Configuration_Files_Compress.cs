using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Configuration
{
	public class Configuration_Files_Compress : IConfiguration
	{
		public Compressor.Type CompressorType { get; set; }
	}
}
