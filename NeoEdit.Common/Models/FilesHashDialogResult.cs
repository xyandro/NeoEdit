using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Models
{
	public class FilesHashDialogResult
	{
		public Hasher.Type HashType { get; set; }
		public byte[] HMACKey { get; set; }
	}
}
