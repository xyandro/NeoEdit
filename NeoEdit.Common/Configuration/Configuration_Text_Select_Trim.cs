using System.Collections.Generic;

namespace NeoEdit.Common.Configuration
{
	public class Configuration_Text_Select_Trim : IConfiguration
	{
		public HashSet<char> TrimChars { get; set; }
		public bool Start { get; set; }
		public bool End { get; set; }
	}
}
