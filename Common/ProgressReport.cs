namespace NeoEdit
{
	public class ProgressReport
	{
		public double Value { get; set; }
		public double Maximum { get; set; }

		public ProgressReport(double value, double maximum)
		{
			Value = value;
			Maximum = maximum;
		}
	}
}
