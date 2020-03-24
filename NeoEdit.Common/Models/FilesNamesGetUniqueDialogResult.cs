namespace NeoEdit.Common.Models
{
	public class FilesNamesGetUniqueDialogResult
	{
		public string Format { get; set; }
		public bool CheckExisting { get; set; }
		public bool RenameAll { get; set; }
		public bool UseGUIDs { get; set; }
	}
}
