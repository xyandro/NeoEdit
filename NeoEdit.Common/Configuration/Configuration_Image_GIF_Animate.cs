namespace NeoEdit.Common.Configuration
{
	public class Configuration_Image_GIF_Animate : IConfiguration
	{
		public string InputFiles { get; set; }
		public string OutputFile { get; set; }
		public string Delay { get; set; }
		public string Repeat { get; set; }
	}
}
