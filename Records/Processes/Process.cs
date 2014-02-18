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
		}
	}
}
