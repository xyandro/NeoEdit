namespace NeoEdit.Records.Processes
{
	public class Process : ProcessRecord
	{
		public Process(System.Diagnostics.Process process)
			: base(process.Id.ToString())
		{
			SetProperty(RecordProperty.PropertyName.ProcessName, process.ProcessName);
		}
	}
}
