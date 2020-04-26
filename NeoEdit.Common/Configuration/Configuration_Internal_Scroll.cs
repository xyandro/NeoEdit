namespace NeoEdit.Common.Configuration
{
	public class Configuration_Internal_Scroll : IConfiguration
	{
		public ITab Tab { get; set; }
		public int Column { get; set; }
		public int Row { get; set; }
	}
}
