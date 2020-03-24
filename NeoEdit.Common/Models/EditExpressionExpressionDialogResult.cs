namespace NeoEdit.Common.Models
{
	public class EditExpressionExpressionDialogResult
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
