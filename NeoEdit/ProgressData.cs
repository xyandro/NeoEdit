namespace NeoEdit.Program
{
	public class ProgressData
	{
		public string Name { get; set; }
		public int Percent { get; set; }
		public bool Cancel { get; set; }
		public bool Done { get; set; } = true;
	}
}
