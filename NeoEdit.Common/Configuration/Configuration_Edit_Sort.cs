using NeoEdit.Common.Enums;

namespace NeoEdit.Common.Configuration
{
	public class Configuration_Edit_Sort : IConfiguration
	{
		public SortScope SortScope { get; set; }
		public int UseRegion { get; set; }
		public SortType SortType { get; set; }
		public bool CaseSensitive { get; set; }
		public bool Ascending { get; set; }
	}
}
