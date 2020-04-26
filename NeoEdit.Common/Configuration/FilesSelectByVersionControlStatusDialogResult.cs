using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Configuration
{
	public class FilesSelectByVersionControlStatusDialogResult : IConfiguration
	{
		public Versioner.Status Statuses { get; set; }
	}
}
