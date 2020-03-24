using System.Collections.Generic;

namespace NeoEdit.Common.Models
{
	public class NumericConvertBaseDialogResult
	{
		public Dictionary<char, int> InputSet { get; set; }
		public Dictionary<int, char> OutputSet { get; set; }
	}
}
