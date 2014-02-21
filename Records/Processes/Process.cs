using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using NeoEdit.Interop;

namespace NeoEdit.Records.Processes
{
	public class Process : ProcessRecord
	{
		readonly Process parent;
		public Process(int pid, Process _parent = null)
			: base("Process/" + pid.ToString())
		{
			parent = _parent;
			var process = System.Diagnostics.Process.GetProcessById(pid);
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
			this[RecordProperty.PropertyName.CPU] = GetProcessUsage(process.Id);
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
				using (var mos = new ManagementObjectSearcher("SELECT * From Win32_Process WHERE ParentProcessID=" + GetProperty<int>(RecordProperty.PropertyName.ID)))
				using (var moc = mos.Get())
					foreach (var mo in moc)
						yield return new Process(Convert.ToInt32(mo["ProcessID"]), this);
			}
		}

		public override System.Collections.Generic.IEnumerable<RecordAction.ActionName> Actions
		{
			get
			{
				return new List<RecordAction.ActionName> { 
					RecordAction.ActionName.Open,
					RecordAction.ActionName.Delete,
					RecordAction.ActionName.Suspend,
					RecordAction.ActionName.Resume,
				}.Concat(base.Actions);
			}
		}

		public override bool IsFile { get { return true; } }

		public override void Suspend()
		{
			NEInterop.SuspendProcess(GetProperty<int>(RecordProperty.PropertyName.ID));
		}

		public override void Resume()
		{
			NEInterop.ResumeProcess(GetProperty<int>(RecordProperty.PropertyName.ID));
		}

		public override Common.BinaryData Read()
		{
			return new ProcessBinaryData(GetProperty<int>(RecordProperty.PropertyName.ID));
		}

		public override void Delete()
		{
			var process = System.Diagnostics.Process.GetProcessById(GetProperty<int>(RecordProperty.PropertyName.ID));
			process.Kill();
		}
	}
}
