namespace NeoEdit.Common
{
	public class TaskProgress
	{
		public string Name { get; set; }
		public double? Percent { get; set; }
		public bool Cancel { get; set; }
		public bool Working { get; set; }
	}
}
