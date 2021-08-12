using System.Drawing.Drawing2D;

namespace NeoEdit.Common.Configuration
{
	public class Configuration_Image_Size : IConfiguration
	{
		public string WidthExpression { get; set; }
		public string HeightExpression { get; set; }
		public InterpolationMode InterpolationMode { get; set; }
	}
}
