namespace NeoEdit.Common.Configuration
{
	public class NumericCycleDialogResult : IConfiguration
	{
		public string Minimum { get; set; }
		public string Maximum { get; set; }
		public bool IncludeBeginning { get; set; }
	}
}
