using System.Collections.Generic;

namespace NeoEdit.Common.Configuration
{
	public class TextTrimDialogResult : IConfiguration
	{
		public HashSet<char> TrimChars { get; set; }
		public bool Start { get; set; }
		public bool End { get; set; }
	}
}
