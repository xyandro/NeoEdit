namespace NeoEdit.TaskRunning
{
	public interface ITaskRunnerProgress
	{
		long Current { get; set; }
		long Total { get; set; }
	}
}
