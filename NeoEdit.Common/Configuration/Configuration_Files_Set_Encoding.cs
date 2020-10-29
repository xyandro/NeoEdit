using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Configuration
{
	public class Configuration_Files_Set_Encoding : IConfiguration
	{
		public Coder.CodePage InputCodePage { get; set; }
		public Coder.CodePage OutputCodePage { get; set; }
	}
}
