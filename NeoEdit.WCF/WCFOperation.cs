using System.Collections.Generic;

namespace NeoEdit.WCF
{
	public class WCFOperation
	{
		public string ServiceURL { get; set; }
		public string Namespace { get; set; }
		public string Contract { get; set; }
		public string Operation { get; set; }
		public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
		public object Result { get; set; }
	}
}
