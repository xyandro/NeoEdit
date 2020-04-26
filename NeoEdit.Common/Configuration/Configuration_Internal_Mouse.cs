namespace NeoEdit.Common.Configuration
{
	public class Configuration_Internal_Mouse : IConfiguration
	{
		public ITab Tab { get; set; }
		public int Line { get; set; }
		public int Column { get; set; }
		public int ClickCount { get; set; }
		public bool? Selecting { get; set; }
	}
}
