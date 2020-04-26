namespace NeoEdit.Common.Configuration
{
	public class FilesNamesMakeAbsoluteRelativeDialogResult : IConfiguration
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
