using System;
using System.Collections.Generic;

namespace NeoEdit.Common.Models
{
	public class WindowActiveTabsDialogData
	{
		public IReadOnlyList<string> AllTabs { get; set; }
		public IReadOnlyList<int> ActiveIndexes { get; set; }
		public int FocusedIndex { get; set; }
		public Action<List<int>> SetActiveIndexes { get; set; }
		public Action<List<int>> CloseTabs { get; set; }
		public Action<List<(int, int)>> DoMoves { get; set; }
	}
}
