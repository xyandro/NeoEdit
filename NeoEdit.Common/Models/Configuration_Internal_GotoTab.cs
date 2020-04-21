namespace NeoEdit.Common.Models
{
	public class Configuration_Internal_GotoTab
	{
		public ITab Tab { get; set; }
		public int? Line { get; set; }
		public int? Column { get; set; }
		public int? Index { get; set; }
	}
}
