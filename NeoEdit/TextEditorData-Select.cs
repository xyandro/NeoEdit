using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace NeoEdit.Program
{
	partial class TextEditorData
	{
		void Command_Select_All() => SetSelections(new List<Range> { new Range(Text.Length, 0) });

		void Command_Select_Lines()
		{
			var lineSets = Selections.AsParallel().Select(range => new { start = TextView.GetPositionLine(range.Start), end = TextView.GetPositionLine(Math.Max(range.Start, range.End - 1)) }).ToList();

			var hasLine = new bool[TextView.NumLines];
			foreach (var set in lineSets)
				for (var ctr = set.start; ctr <= set.end; ++ctr)
					hasLine[ctr] = true;

			var lines = new List<int>();
			for (var line = 0; line < hasLine.Length; ++line)
				if (hasLine[line])
					lines.Add(line);

			SetSelections(lines.AsParallel().AsOrdered().Select(line => Range.FromIndex(TextView.GetPosition(line, 0), TextView.GetLineLength(line))).ToList());
		}
	}
}
