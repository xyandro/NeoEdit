using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Models
{
	public class EditDataCompressDialogResult
	{
		public Coder.CodePage InputCodePage { get; set; }
		public Compressor.Type CompressorType { get; set; }
		public Coder.CodePage OutputCodePage { get; set; }
	}
}
