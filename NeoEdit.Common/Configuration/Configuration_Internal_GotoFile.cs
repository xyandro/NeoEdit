namespace NeoEdit.Common.Configuration
{
	public class Configuration_Internal_GotoFile : IConfiguration
	{
		public INEFile NEFile { get; set; }
		public int? Line { get; set; }
		public int? Column { get; set; }
		public int? Index { get; set; }
	}
}
