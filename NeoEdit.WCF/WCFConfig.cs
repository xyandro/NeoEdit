using System.Collections.Generic;

namespace NeoEdit.WCF
{
	class WCFConfig
	{
		public List<WCFOperation> Operations { get; } = new List<WCFOperation>();
		public string Config { get; set; }
	}
}
