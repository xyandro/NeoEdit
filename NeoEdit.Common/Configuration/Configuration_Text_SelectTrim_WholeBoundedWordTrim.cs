using System.Collections.Generic;

namespace NeoEdit.Common.Configuration
{
	public class Configuration_Text_SelectTrim_WholeBoundedWordTrim : IConfiguration
	{
		public HashSet<char> Chars { get; set; }
		public bool Start { get; set; }
		public bool End { get; set; }
	}
}
