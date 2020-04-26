namespace NeoEdit.Common.Configuration
{
	public class FilesOperationsSplitFileDialogResult : IConfiguration
	{
		public string OutputTemplate { get; set; }
		public string ChunkSize { get; set; }
	}
}
