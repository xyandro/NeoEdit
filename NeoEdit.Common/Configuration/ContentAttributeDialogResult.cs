using System.Text.RegularExpressions;

namespace NeoEdit.Common.Configuration
{
	public class ContentAttributeDialogResult : IConfiguration
	{
		public string Attribute { get; set; }
		public Regex Regex { get; set; }
		public bool Invert { get; set; }
	}
}
