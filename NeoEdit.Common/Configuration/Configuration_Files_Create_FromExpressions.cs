using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Configuration
{
	public class Configuration_Files_Create_FromExpressions : IConfiguration
	{
		public string FileName { get; set; }
		public string Data { get; set; }
		public Coder.CodePage CodePage { get; set; }
	}
}
