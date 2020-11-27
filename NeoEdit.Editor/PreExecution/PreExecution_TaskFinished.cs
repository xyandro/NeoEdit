namespace NeoEdit.Editor.PreExecution
{
	public class PreExecution_TaskFinished : IPreExecution
	{
		public static PreExecution_TaskFinished Singleton { get; } = new PreExecution_TaskFinished();

		PreExecution_TaskFinished() { }
	}
}
