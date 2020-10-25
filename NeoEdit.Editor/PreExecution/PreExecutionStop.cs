namespace NeoEdit.Editor.PreExecution
{
	public class PreExecutionStop : IPreExecution
	{
		static public PreExecutionStop Stop { get; } = new PreExecutionStop();

		private PreExecutionStop() { }
	}
}
