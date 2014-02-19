using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Records.Processes
{
	public class Process : ProcessRecord
	{
		public Process(System.Diagnostics.Process process)
			: base(process.Id.ToString())
		{
			this[RecordProperty.PropertyName.ID] = process.Id;
			this[RecordProperty.PropertyName.Name] = process.ProcessName;
			var data = "";
			if (process.Id == 0)
				data = "Idle";
			else if (process.Id == 4)
				data = "System";
			else
				try { data = process.MainModule.FileVersionInfo.FileDescription; }
				catch { }
			this[RecordProperty.PropertyName.Data] = data;
			this[RecordProperty.PropertyName.Size] = process.WorkingSet64;
			this[RecordProperty.PropertyName.CPU] = GetProcessUsage(process.Id);
		}

		public override System.Collections.Generic.IEnumerable<RecordAction.ActionName> Actions
		{
			get
			{
				return new List<RecordAction.ActionName> { 
					RecordAction.ActionName.Open,
				}.Concat(base.Actions);
			}
		}

		public override bool IsFile { get { return true; } }

		public override Common.IBinaryData Read()
		{
			return new ProcessBinaryData(GetProperty<int>(RecordProperty.PropertyName.ID));
		}
	}
}
