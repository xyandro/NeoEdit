using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Configuration
{
	public class EncodingDialogResult : IConfiguration
	{
		public Coder.CodePage CodePage { get; set; }
	}
}
