namespace NeoEdit.Common.Configuration
{
	public class Configuration_Text_SelectWidth_ByWidth : IConfiguration
	{
		public enum TextLocation
		{
			Start,
			Middle,
			End,
		}

		public string Expression { get; set; }
		public char PadChar { get; set; }
		public TextLocation Location { get; set; }
	}
}
