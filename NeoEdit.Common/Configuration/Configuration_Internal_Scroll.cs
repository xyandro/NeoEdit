namespace NeoEdit.Common.Configuration
{
	public class Configuration_Internal_Scroll : IConfiguration
	{
		public INEFile NEFile { get; set; }
		public int Column { get; set; }
		public int Row { get; set; }
	}
}
