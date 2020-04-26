namespace NeoEdit.Common.Configuration
{
	public class Configuration_Image_Crop : IConfiguration
	{
		public string XExpression { get; set; }
		public string YExpression { get; set; }
		public string WidthExpression { get; set; }
		public string HeightExpression { get; set; }
		public string FillColor { get; set; }
	}
}
