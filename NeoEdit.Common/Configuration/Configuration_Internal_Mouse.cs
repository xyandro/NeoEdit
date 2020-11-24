namespace NeoEdit.Common.Configuration
{
	public class Configuration_Internal_Mouse : IConfiguration
	{
		public INEFile NEFile { get; set; }
		public bool ActivateOnly { get; set; }
		public int Line { get; set; }
		public int Column { get; set; }
		public int ClickCount { get; set; }
		public bool Selecting { get; set; }
	}
}
