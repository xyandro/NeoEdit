using System.Collections.Generic;

namespace NeoEdit.Records.Processes
{
	public class ProcessRoot : ProcessRecord
	{
		public ProcessRoot() : base("Processes") { }

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
