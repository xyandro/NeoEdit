using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace NeoEdit.Records.Processes
{
	public class ProcessRecord : Record
	{
		static object lockObj = new object();
		static long lastTicks;
		static long curTicks;
		static Dictionary<int, long> lastUsage;
		static Dictionary<int, long> curUsage;
		static ProcessRecord()
		{
			var timer = new Timer(1000);
			timer.Elapsed += (s, e) =>
			{
				lock (lockObj)
				{
					lastTicks = curTicks;
					lastUsage = curUsage;
					curTicks = DateTime.Now.Ticks;
					curUsage = System.Diagnostics.Process.GetProcesses().Where(process => process.Id != 0).ToDictionary(process => process.Id, process => process.TotalProcessorTime.Ticks);
				}
			};
			timer.Start();
		}

		protected double GetProcessUsage(int PID)
		{
			lock (lockObj)
			{
				if ((lastTicks == 0) || (curTicks == 0))
					return 0;
				if ((!lastUsage.ContainsKey(PID)) || (!curUsage.ContainsKey(PID)))
					return 0;

				return (double)(curUsage[PID] - lastUsage[PID]) / (curTicks - lastTicks) * 100;
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
