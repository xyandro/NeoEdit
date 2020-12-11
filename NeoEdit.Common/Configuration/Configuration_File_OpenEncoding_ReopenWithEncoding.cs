using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Configuration
{
	public class Configuration_File_OpenEncoding_ReopenWithEncoding : IConfiguration
	{
		public Coder.CodePage CodePage { get; set; }
		public bool HasBOM { get; set; }
	}
}
