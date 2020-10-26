using System;
using System.Collections.Generic;

namespace NeoEdit.Common.Models
{
	public class WindowActiveFilesDialogData
	{
		public IReadOnlyList<string> AllFiles { get; set; }
		public IReadOnlyList<int> ActiveIndexes { get; set; }
		public int FocusedIndex { get; set; }
		public Action<List<int>> SetActiveIndexes { get; set; }
		public Action<List<int>> CloseFiles { get; set; }
		public Action<List<(int, int)>> DoMoves { get; set; }
	}
}
