namespace NeoEdit.Common.Models
{
	public class TextWidthDialogResult
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
