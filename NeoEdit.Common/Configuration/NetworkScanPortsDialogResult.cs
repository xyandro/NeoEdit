using System;
using System.Collections.Generic;

namespace NeoEdit.Common.Configuration
{
	public class NetworkScanPortsDialogResult : IConfiguration
	{
		public List<Tuple<int, int>> Ports { get; set; }
		public int Attempts { get; set; }
		public int Timeout { get; set; }
		public int Concurrency { get; set; }
	}
}
