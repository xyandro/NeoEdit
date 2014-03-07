using System;
using System.Collections.Generic;

namespace NeoEdit.GUI.Records.Processes
{
	public class ProcessRoot : ProcessRecord
	{
		public ProcessRoot() : base("Processes") { }

		public override Record GetRecord(string uri)
		{
			if (!uri.StartsWith(@"Process\"))
				return null;
			return new Process(Convert.ToInt32(uri.Substring(8)));
		}

		public override IEnumerable<Record> Records
		{
			get
			{
				foreach (var process in System.Diagnostics.Process.GetProcesses())
					yield return new Process(process.Id);
			}
		}
	}
}
