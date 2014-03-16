using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Timers;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.ItemGridControl;

namespace NeoEdit.Processes
{
	public class ProcessItem : ItemGridItem<ProcessItem>
	{
		[DepProp]
		public int PID { get { return GetValue<int>(); } private set { SetValue(value); } }
		[DepProp]
		public string Name { get { return GetValue<string>(); } private set { SetValue(value); } }
		[DepProp]
		public string Title { get { return GetValue<string>(); } private set { SetValue(value); } }
		[DepProp]
		public long Size { get { return GetValue<long>(); } private set { SetValue(value); } }
		[DepProp]
		public double CPU { get { return GetValue<double>(); } private set { SetValue(value); } }
		[DepProp]
		public int ParentPID { get { return GetValue<int>(); } private set { SetValue(value); } }

		static object lockObj = new object();
		static Dictionary<int, double> usage = new Dictionary<int, double>();
		static Dictionary<int, PerformanceCounter> counter = new Dictionary<int, PerformanceCounter>();
		static ProcessItem()
		{
			var timer = new Timer(1000);
			timer.Elapsed += (s, e) =>
			{
				lock (lockObj)
				{
					var processes = Process.GetProcesses();
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

		ProcessItem() { }

		public static List<ProcessItem> GetProcesses()
		{
			var netProcess = Process.GetProcesses().ToDictionary(process => process.Id, process => process);

			var processes = new List<ProcessItem>();
			using (var mos = new ManagementObjectSearcher("SELECT ProcessId, Name, WorkingSetSize, ParentProcessID From Win32_Process"))
			using (var moc = mos.Get())
				lock (lockObj)
					foreach (var mo in moc)
					{
						var PID = Convert.ToInt32(mo["ProcessID"]);
						processes.Add(new ProcessItem
						{
							PID = PID,
							Name = mo["Name"].ToString(),
							Size = Convert.ToInt64(mo["WorkingSetSize"]),
							ParentPID = Convert.ToInt32(mo["ParentProcessID"]),
							CPU = usage.ContainsKey(PID) ? Math.Round(usage[PID], 1) : 0,
							Title = netProcess.ContainsKey(PID) ? netProcess[PID].MainWindowTitle : null,
						});
					}

			return processes;
		}
	}
}
