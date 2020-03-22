namespace NeoEdit.Program.Models
{
	public class FilesNamesMakeAbsoluteRelativeDialogResult
	{
		public enum ResultType
		{
			None,
			File,
			Directory,
		}

		public string Expression { get; set; }
		public ResultType Type { get; set; }
	}
}
