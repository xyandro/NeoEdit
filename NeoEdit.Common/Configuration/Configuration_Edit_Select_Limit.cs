namespace NeoEdit.Common.Configuration
{
	public class Configuration_Edit_Select_Limit : IConfiguration
	{
		public string FirstSelection { get; set; }
		public string EveryNth { get; set; }
		public string TakeCount { get; set; }
		public string NumSelections { get; set; }
		public bool JoinSelections { get; set; }
	}
}
