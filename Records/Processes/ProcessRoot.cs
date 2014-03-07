using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NeoEdit.Records.Processes
{
	public class ProcessRoot : ProcessRecord
	{
		public ProcessRoot() : base("Processes") { }

		public override Record GetRecord(string uri)
		{
			if (!uri.StartsWith(@"Process\"))
				return null;
			return new ProcessItem(Convert.ToInt32(uri.Substring(8)));
		}

		public override IEnumerable<Record> Records
		{
			get
			{
				foreach (var process in Process.GetProcesses())
					yield return new ProcessItem(process.Id);
			}
		}
	}
}
