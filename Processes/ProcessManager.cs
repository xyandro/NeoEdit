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
		static Dictionary<int, long> lastTicks;
		static Dictionary<int, double> usage;
		static DateTime lastCollectionTime;

		static public void WindowCreated()
		{
			lock (lockObj)
			{
				if (windowCount == 0)
				{
					lastTicks = new Dictionary<int, long>();
					usage = new Dictionary<int, double>();
					lastCollectionTime = DateTime.UtcNow;

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
					lastTicks = null;
					usage = null;
					lastCollectionTime = default(DateTime);
				}
			}
		}

		static void CollectProcesses(object sender, ElapsedEventArgs e)
		{
			lock (lockObj)
			{
				var currentCollectionTime = DateTime.UtcNow;
				var totalTicks = currentCollectionTime.Ticks - lastCollectionTime.Ticks;
				lastCollectionTime = currentCollectionTime;

				var processes = Process.GetProcesses();
				var currentTicks = new Dictionary<int, long>();
				foreach (var process in processes)
					try { currentTicks[process.Id] = process.TotalProcessorTime.Ticks; }
					catch { }
				usage = currentTicks.Where(pair => lastTicks.ContainsKey(pair.Key)).ToDictionary(pair => pair.Key, pair => (double)(pair.Value - lastTicks[pair.Key]) / totalTicks);
				lastTicks = currentTicks;
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
