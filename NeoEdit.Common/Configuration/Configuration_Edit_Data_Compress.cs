using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Configuration
{
	public class Configuration_Edit_Data_Compress : IConfiguration
	{
		public Coder.CodePage InputCodePage { get; set; }
		public Compressor.Type CompressorType { get; set; }
		public Coder.CodePage OutputCodePage { get; set; }
	}
}
