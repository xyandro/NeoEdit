namespace NeoEdit.Common.Configuration
{
	public class Configuration_Edit_Expression_Expression : IConfiguration
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
