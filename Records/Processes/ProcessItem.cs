using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using NeoEdit.Records.Handles;
using NeoEdit.Win32;

namespace NeoEdit.Records.Processes
{
	public class ProcessItem : ProcessRecord
	{
		readonly ProcessItem parent;
		public ProcessItem(int pid, ProcessItem _parent = null)
			: base(@"Process\" + pid.ToString())
		{
			parent = _parent;
			var process = Process.GetProcessById(pid);
			this[RecordProperty.PropertyName.ID] = pid;
			this[RecordProperty.PropertyName.Name] = process.ProcessName;
			var data = "";
			if (process.Id == 0)
				data = "Idle";
			else if (process.Id == 4)
				data = "System";
			else
				try { data = process.MainModule.FileVersionInfo.FileDescription; }
				catch { }
			if (!String.IsNullOrWhiteSpace(process.MainWindowTitle))
				data += " (" + process.MainWindowTitle + ")";
			this[RecordProperty.PropertyName.Data] = data;
			this[RecordProperty.PropertyName.Size] = process.WorkingSet64;
			this[RecordProperty.PropertyName.CPU] = GetProcessUsage();
		}

		public override Record Parent
		{
			get
			{
				if (parent != null)
					return parent;
				return base.Parent;
			}
		}

		public override IEnumerable<Record> Records
		{
			get
			{
				using (var mos = new ManagementObjectSearcher("SELECT ProcessID From Win32_Process WHERE ParentProcessID=" + GetProperty<int>(RecordProperty.PropertyName.ID)))
				using (var moc = mos.Get())
					foreach (var mo in moc)
						yield return new ProcessItem(Convert.ToInt32(mo["ProcessID"]), this);
				var handles = Interop.GetProcessHandles(GetProperty<int>(RecordProperty.PropertyName.ID));
				foreach (var handle in handles)
					yield return new HandleItem(handle);
			}
		}

		public override bool IsFile { get { return true; } }
		public override bool CanOpen() { return true; }
		public override bool CanDelete() { return true; }
		public override bool CanSuspend() { return true; }
		public override bool CanResume() { return true; }

		public override void Suspend()
		{
			Interop.SuspendProcess(GetProperty<int>(RecordProperty.PropertyName.ID));
		}

		public override void Resume()
		{
			Interop.ResumeProcess(GetProperty<int>(RecordProperty.PropertyName.ID));
		}

		//public override BinaryData Read()
		//{
		//	return new ProcessBinaryData(GetProperty<int>(RecordProperty.PropertyName.ID));
		//}

		public override void Delete()
		{
			var process = Process.GetProcessById(GetProperty<int>(RecordProperty.PropertyName.ID));
			process.Kill();
		}
	}
}
