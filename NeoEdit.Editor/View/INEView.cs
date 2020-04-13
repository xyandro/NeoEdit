using NeoEdit.Common;

namespace NeoEdit.Editor.View
{
	public interface INEView
	{
		string DefaultEnding { get; }
		int MaxIndex { get; }
		int MaxPosition { get; }
		int NumLines { get; }

		int GetColumnFromIndex(NEText text, int line, int findIndex);
		Range GetEnding(int line);
		int GetEndingLength(int line);
		int GetIndexFromColumn(NEText text, int line, int findColumn, bool returnMaxOnFail = false);
		Range GetLine(int line, bool includeEnding = false);
		string GetLineColumns(NEText text, int line, int startColumn, int endColumn);
		int GetLineColumnsLength(NEText text, int line);
		int GetLineLength(int line);
		int GetMaxColumn(NEText text);
		int GetPosition(int line, int index, bool allowJustPastEnd = false);
		int GetPositionIndex(int position, int line);
		int GetPositionLine(int position);
	}
}
