using System.Collections.Generic;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Transform;

namespace NeoEdit.Common
{
	public interface ITab
	{
		int MaxColumn { get; }
		int StartColumn { get; }
		int NumLines { get; }
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

		void GetViewBinaryData(out byte[] data, out bool hasSel);
		int GetPosition(int line, int index, bool allowJustPastEnd = false);
		int GetIndexFromColumn(int line, int findColumn, bool returnMaxOnFail = false);
		int GetLineLength(int line);
		int GetPositionLine(int position);
		int GetPositionIndex(int position, int line);
		int GetColumnFromIndex(int line, int findIndex);
		int GetLineColumnsLength(int line);
		string GetLineColumns(int line, int startColumn, int endColumn);
		void SetTabSize(int columns, int rows);
		IReadOnlyList<Range> GetRegions(int region);
		List<string> GetStatusBar();
	}
}
