using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Windows;
using System.Timers;

namespace NeoEdit.Processes
{
	public class ProcessItem : DependencyObject
	{
		public int PID { get { return GetProperty<int>(Property.PID); } }
		public enum Property
		{
			PID,
			Name,
			Title,
			Size,
			CPU,
			ParentPID,
		}

		static Dictionary<Property, Type> propertyType = new Dictionary<Property, Type>
		{
			{ Property.PID, typeof(int?) },
			{ Property.Name, typeof(string) },
			{ Property.Title, typeof(string) },
			{ Property.Size, typeof(long?) },
			{ Property.CPU, typeof(double?) },
			{ Property.ParentPID, typeof(int?) },
		};
		static Dictionary<Property, DependencyProperty> dependencyProperty;
		static object lockObj = new object();
		static Dictionary<int, double> usage = new Dictionary<int, double>();
		static Dictionary<int, PerformanceCounter> counter = new Dictionary<int, PerformanceCounter>();
		static ProcessItem()
		{
			dependencyProperty = propertyType.ToDictionary(a => a.Key, a => DependencyProperty.Register(a.Key.ToString(), a.Value, typeof(ProcessItem)));
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

		public static Type PropertyType(Property property)
		{
			return propertyType[property];
		}

		public static DependencyProperty GetDepProp(Property property)
		{
			return dependencyProperty[property];
		}

		ProcessItem(int PID)
		{
			SetProperty(Property.PID, PID);
		}

		public static void UpdateProcesses(ObservableCollection<ProcessItem> processes)
		{
			var netProcess = Process.GetProcesses().ToDictionary(process => process.Id, process => process);
			var processesByPID = processes.ToDictionary(process => process.PID, process => process);
			var found = new HashSet<int>();

			using (var mos = new ManagementObjectSearcher("SELECT ProcessId, Name, WorkingSetSize, ParentProcessID From Win32_Process"))
			using (var moc = mos.Get())
				foreach (var mo in moc)
				{
					var PID = Convert.ToInt32(mo["ProcessID"]);
					found.Add(PID);
					if (!processesByPID.ContainsKey(PID))
						processes.Add(processesByPID[PID] = new ProcessItem(PID));
					var process = processesByPID[PID];

					process.SetProperty(Property.Name, mo["Name"]);
					process.SetProperty(Property.Size, Convert.ToInt64(mo["WorkingSetSize"]));
					process.SetProperty(Property.ParentPID, Convert.ToInt32(mo["ParentProcessID"]));
					if (netProcess.ContainsKey(PID)) process.SetProperty(Property.Title, netProcess[PID].MainWindowTitle);
				}

			var extra = processes.Where(process => !found.Contains(process.PID)).ToList();
			extra.ForEach(process => processes.Remove(process));

			lock (lockObj)
				foreach (var process in processes)
					process.SetProperty(Property.CPU, usage.ContainsKey(process.PID) ? Math.Round(usage[process.PID], 1) : 0);
		}

		void SetProperty(Property property, object value)
		{
			if ((value is string) && (((string)value).Length == 0))
				value = null;
			SetValue(dependencyProperty[property], value);
		}

		public T GetProperty<T>(Property property)
		{
			return (T)GetValue(dependencyProperty[property]);
		}
	}
}
