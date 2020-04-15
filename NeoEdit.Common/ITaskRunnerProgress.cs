namespace NeoEdit.Common
{
	public interface ITaskRunnerProgress
	{
		void SetProgress(long current, long total);
	}
}
