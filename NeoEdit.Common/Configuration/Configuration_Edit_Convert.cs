using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Configuration
{
	public class Configuration_Edit_Convert : IConfiguration
	{
		public Coder.CodePage InputType { get; set; }
		public bool InputBOM { get; set; }
		public Coder.CodePage OutputType { get; set; }
		public bool OutputBOM { get; set; }
	}
}
