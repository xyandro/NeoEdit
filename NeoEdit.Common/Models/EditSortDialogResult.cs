using NeoEdit.Common.Enums;

namespace NeoEdit.Common.Models
{
	public class EditSortDialogResult
	{
		public SortScope SortScope { get; set; }
		public int UseRegion { get; set; }
		public SortType SortType { get; set; }
		public bool CaseSensitive { get; set; }
		public bool Ascending { get; set; }
	}
}
