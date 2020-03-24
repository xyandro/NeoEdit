namespace NeoEdit.Common.Models
{
	public class MacroPlayRepeatDialogResult
	{
		public enum RepeatTypeEnum
		{
			Number,
			Condition,
		}

		public string Macro { get; set; }
		public string Expression { get; set; }
		public RepeatTypeEnum RepeatType { get; set; }
	}
}
