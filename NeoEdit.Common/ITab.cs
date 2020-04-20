using System;
using System.Collections.Generic;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Transform;

namespace NeoEdit.Common
{
	public interface ITab
	{
		int ViewMaxColumn { get; }
		int StartColumn { get; }
		int ViewNumLines { get; }
		int StartRow { get; }
		bool ViewBinary { get; }
		IReadOnlyList<HashSet<string>> ViewBinarySearches { get; }
		HashSet<Coder.CodePage> ViewBinaryCodePages { get; }
		IReadOnlyList<Range> Selections { get; }
		int CurrentSelection { get; }
		bool HighlightSyntax { get; }
		ParserType ContentType { get; }
		string TabLabel { get; }
		string DisplayName { get; }
		string FileName { get; }

		void ViewGetBinaryData(out byte[] data, out bool hasSel);
		int ViewGetPosition(int line, int index, bool allowJustPastEnd = false);
		int ViewGetIndexFromColumn(int line, int findColumn, bool returnMaxOnFail = false);
		int ViewGetLineLength(int line);
		int ViewGetPositionLine(int position);
		int ViewGetPositionIndex(int position, int line);
		int ViewGetColumnFromIndex(int line, int findIndex);
		int ViewGetLineColumnsLength(int line);
		string ViewGetLineColumns(int line, int startColumn, int endColumn);
		void ViewSetTabSize(int columns, int rows);
		IReadOnlyList<Range> GetRegions(int region);
		List<string> ViewGetStatusBar();
		DiffType ViewGetLineDiffType(int line);
		List<int> ViewGetLineColumnMap(int line, bool includeEnding = false);
		List<Tuple<int, int>> ViewGetLineColumnDiffs(int line);
	}
}
