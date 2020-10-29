using NeoEdit.Common.Transform;

namespace NeoEdit.Common.Configuration
{
	public class Configuration_Files_Select_BySourceControlStatus : IConfiguration
	{
		public Versioner.Status Statuses { get; set; }
	}
}
