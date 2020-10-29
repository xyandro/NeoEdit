namespace NeoEdit.Common.Configuration
{
	public class Configuration_Text_Select_Split : IConfiguration
	{
		public string Text { get; set; }
		public string Index { get; set; }
		public bool WholeWords { get; set; }
		public bool MatchCase { get; set; }
		public bool IsRegex { get; set; }
		public bool IncludeResults { get; set; }
		public bool ExcludeEmpty { get; set; }
		public bool BalanceStrings { get; set; }
		public bool BalanceParens { get; set; }
		public bool BalanceBrackets { get; set; }
		public bool BalanceBraces { get; set; }
		public bool BalanceLTGT { get; set; }
		public bool TrimWhitespace { get; set; }
	}
}
