namespace NeoEdit.Editor.PreExecution
{
	public class PreExecutionStop : IPreExecution
	{
		public static PreExecutionStop Stop { get; } = new PreExecutionStop();

		private PreExecutionStop() { }
	}
}
