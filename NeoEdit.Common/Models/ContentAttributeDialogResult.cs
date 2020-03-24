using System.Text.RegularExpressions;

namespace NeoEdit.Common.Models
{
	public class ContentAttributeDialogResult
	{
		public string Attribute { get; set; }
		public Regex Regex { get; set; }
		public bool Invert { get; set; }
	}
}
