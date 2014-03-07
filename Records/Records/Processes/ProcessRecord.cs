using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;

namespace NeoEdit.GUI.Records.Processes
{
	public class ProcessRecord : Record
	{
		static object lockObj = new object();
		static Dictionary<int, PerformanceCounter> counter = new Dictionary<int, PerformanceCounter>();
		static Dictionary<int, double> usage = new Dictionary<int, double>();
		static ProcessRecord()
		{
			var timer = new Timer(1000);
			timer.Elapsed += (s, e) =>
			{
				lock (lockObj)
				{
					var processes = System.Diagnostics.Process.GetProcesses();
					counter.Where(ctr => !processes.Any(process => process.Id == ctr.Key)).ToList().ForEach(ctr => counter.Remove(ctr.Key));
					usage.Where(ctr => !processes.Any(process => process.Id == ctr.Key)).ToList().ForEach(ctr => usage.Remove(ctr.Key));
					foreach (var process in processes)
					{
						try
						{
							if (!counter.ContainsKey(process.Id))
								counter[process.Id] = new PerformanceCounter("Process", "% Processor Time", process.ProcessName);
							usage[process.Id] = counter[process.Id].NextValue();
						}
						catch { }
					}
				}
			};
			timer.Start();
		}

		protected double GetProcessUsage()
		{
			lock (lockObj)
			{
				var pid = GetProperty<int>(RecordProperty.PropertyName.ID);
				if (!usage.ContainsKey(pid))
					return 0;
				return Math.Round(usage[pid], 1);
			}
		}

		public ProcessRecord(string uri) : base(uri) { }

		public override Record Parent
		{
			get
			{
				if (this is ProcessRoot)
					return new Root();
				if (this is Process)
					return new ProcessRoot();
				throw new ArgumentException();
			}
		}

		public override Type GetRootType()
		{
			return typeof(ProcessRecord);
		}
	}
}
