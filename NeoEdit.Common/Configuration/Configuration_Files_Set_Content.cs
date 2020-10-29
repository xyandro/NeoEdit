using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Configuration
{
	public class Configuration_Files_Set_Content : IConfiguration
	{
		public string FileName { get; set; }
		public string Data { get; set; }
		public Coder.CodePage CodePage { get; set; }
	}
}
