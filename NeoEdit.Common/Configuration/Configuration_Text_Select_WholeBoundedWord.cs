using System.Collections.Generic;

namespace NeoEdit.Common.Configuration
{
	public class Configuration_Text_Select_WholeBoundedWord : IConfiguration
	{
		public HashSet<char> Chars { get; set; }
		public bool Start { get; set; }
		public bool End { get; set; }
	}
}
