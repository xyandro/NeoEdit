namespace NeoEdit.Common.Configuration
{
	public class FilesNamesGetUniqueDialogResult : IConfiguration
	{
		public string Format { get; set; }
		public bool CheckExisting { get; set; }
		public bool RenameAll { get; set; }
		public bool UseGUIDs { get; set; }
	}
}
