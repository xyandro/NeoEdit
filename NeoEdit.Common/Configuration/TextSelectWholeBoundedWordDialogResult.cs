using System.Collections.Generic;

namespace NeoEdit.Common.Configuration
{
	public class TextSelectWholeBoundedWordDialogResult : IConfiguration
	{
		public HashSet<char> Chars { get; set; }
		public bool Start { get; set; }
		public bool End { get; set; }
	}
}
