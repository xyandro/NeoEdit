namespace NeoEdit.Common.Configuration
{
	public class ImageGIFAnimateDialogResult : IConfiguration
	{
		public string InputFiles { get; set; }
		public string OutputFile { get; set; }
		public string Delay { get; set; }
		public string Repeat { get; set; }
	}
}
