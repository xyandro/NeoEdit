using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Timers;

namespace NeoEdit.Processes
{
	public class ProcessManager
	{
		static object lockObj = new object();

		static int windowCount = 0;
		static Timer timer;
		static Dictionary<int, double> usage;
		static Dictionary<int, PerformanceCounter> counter;

		static public void WindowCreated()
		{
			lock (lockObj)
			{
				if (windowCount == 0)
				{
					usage = new Dictionary<int, double>();
					counter = new Dictionary<int, PerformanceCounter>();

					timer = new Timer(1000);
					timer.Elapsed += CollectProcesses;
					timer.Start();
				}
				++windowCount;
			}
		}

		static public void WindowDestroyed()
		{
			lock (lockObj)
			{
				--windowCount;
				if (windowCount == 0)
				{
					timer.Stop();
					timer.Dispose();
					timer = null;
					usage = null;
					counter = null;
				}
			}
		}

		static void CollectProcesses(object sender, ElapsedEventArgs e)
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
		}

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
