using System.Collections.Generic;

namespace NeoEdit.Common.Configuration
{
	public class NumericConvertBaseDialogResult : IConfiguration
	{
		public Dictionary<char, int> InputSet { get; set; }
		public Dictionary<int, char> OutputSet { get; set; }
	}
}
