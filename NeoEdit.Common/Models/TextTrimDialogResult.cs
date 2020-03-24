using System.Collections.Generic;

namespace NeoEdit.Common.Models
{
	public class TextTrimDialogResult
	{
		public HashSet<char> TrimChars { get; set; }
		public bool Start { get; set; }
		public bool End { get; set; }
	}
}
