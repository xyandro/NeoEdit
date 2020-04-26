using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Configuration
{
	public class FilesCreateFromExpressionsDialogResult : IConfiguration
	{
		public string FileName { get; set; }
		public string Data { get; set; }
		public Coder.CodePage CodePage { get; set; }
	}
}
