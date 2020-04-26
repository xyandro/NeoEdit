using System.Collections.Generic;

namespace NeoEdit.Common.Configuration
{
	public class Configuration_Numeric_ConvertBase : IConfiguration
	{
		public Dictionary<char, int> InputSet { get; set; }
		public Dictionary<int, char> OutputSet { get; set; }
	}
}
