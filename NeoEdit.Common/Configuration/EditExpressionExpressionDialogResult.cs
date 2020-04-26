namespace NeoEdit.Common.Configuration
{
	public class EditExpressionExpressionDialogResult : IConfiguration
	{
		public enum Actions
		{
			Evaluate,
			Copy,
		}

		public string Expression { get; set; }
		public Actions Action { get; set; }
	}
}
