using System.Collections.Generic;

namespace NeoEdit.Common.Models
{
	public class TextSelectWholeBoundedWordDialogResult
	{
		public HashSet<char> Chars { get; set; }
		public bool Start { get; set; }
		public bool End { get; set; }
	}
}
