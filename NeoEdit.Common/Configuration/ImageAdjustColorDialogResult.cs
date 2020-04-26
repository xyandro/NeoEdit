namespace NeoEdit.Common.Configuration
{
	public class ImageAdjustColorDialogResult : IConfiguration
	{
		public string Expression { get; set; }
		public bool Alpha { get; set; }
		public bool Red { get; set; }
		public bool Green { get; set; }
		public bool Blue { get; set; }
	}
}
