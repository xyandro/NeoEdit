using System;
using System.Collections.Generic;
using NeoEdit.Common;
using NeoEdit.Editor.Transactional;

namespace NeoEdit.Editor
{
	class DiffData
	{
		public NEText Text;
		public DiffParams DiffParams;
		public List<DiffType> LineCompare;
		public List<Tuple<int, int>>[] ColCompare;
		public Dictionary<int, int> LineMap, LineRevMap;

		public DiffData(NEText text, DiffParams diffParams)
		{
			Text = text;
			DiffParams = diffParams;
		}
	}
}
