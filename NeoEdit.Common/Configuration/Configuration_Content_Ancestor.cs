using System.Text.RegularExpressions;

namespace NeoEdit.Common.Configuration
{
	public class Configuration_Content_Ancestor : IConfiguration
	{
		public string Attribute { get; set; }
		public Regex Regex { get; set; }
		public bool Invert { get; set; }
	}
}
