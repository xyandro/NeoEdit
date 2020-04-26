using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Configuration
{
	public class FilesOperationsEncodingDialogResult : IConfiguration
	{
		public Coder.CodePage InputCodePage { get; set; }
		public Coder.CodePage OutputCodePage { get; set; }
	}
}
