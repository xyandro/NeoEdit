using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Models
{
	public class EditConvertDialogResult
	{
		public Coder.CodePage InputType { get; set; }
		public bool InputBOM { get; set; }
		public Coder.CodePage OutputType { get; set; }
		public bool OutputBOM { get; set; }
	}
}
