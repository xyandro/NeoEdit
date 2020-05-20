using System;
using NeoEdit.Common;

namespace NeoEdit.Editor.Transactional.View
{
	public class NEHexView : INEView
	{
		public string DefaultEnding => "\r\n";
		public string OnlyEnding => "\r\n";

		public int MaxIndex { get; }
		public int MaxPosition { get; }
		public int NumLines { get; }

		public NEHexView(int maxPosition, int width)
		{
			MaxPosition = maxPosition;
			MaxIndex = width;
			NumLines = (MaxPosition + MaxIndex - 1) / MaxIndex;
		}

		public int GetColumnFromIndex(NEText text, int line, int findIndex)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();
			if ((findIndex < 0) || (findIndex > GetLineLength(line) + 1))
				throw new IndexOutOfRangeException();
			return findIndex;
		}

		public Range GetEnding(int line)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();
			return Range.FromIndex(line * MaxIndex, 0);
		}

		public int GetEndingLength(int line)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();
			return 0;
		}

		public int GetIndexFromColumn(NEText text, int line, int findColumn, bool returnMaxOnFail = false)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();
			if (findColumn < 0)
				throw new IndexOutOfRangeException();

			var lineMax = GetLineLength(line) + 1;
			if (findColumn > lineMax)
				if (returnMaxOnFail)
					findColumn = lineMax;
				else
					throw new IndexOutOfRangeException();

			return findColumn;
		}

		public Range GetLine(int line, bool includeEnding = false)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();
			return Range.FromIndex(line * MaxIndex, GetLineLength(line));
		}

		public string GetLineColumns(NEText text, int line, int startColumn, int endColumn)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();
			return text.GetString(line * MaxIndex + startColumn, Math.Min(endColumn, GetLineLength(line)) - startColumn);
		}

		public int GetLineColumnsLength(NEText text, int line) => GetLineLength(line);

		public int GetLineLength(int line)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();

			if (line < NumLines - 1)
				return MaxIndex;

			return MaxPosition - (NumLines - 1) * MaxIndex;
		}

		public int GetMaxColumn(NEText text) => MaxIndex;

		public int GetPosition(int line, int index, bool allowJustPastEnd = false)
		{
			if ((allowJustPastEnd) && (line == NumLines) && (index == 0))
				return MaxPosition;

			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();
			if ((index < 0) || (index > GetLineLength(line) + 1))
				throw new IndexOutOfRangeException();
			if (index == GetLineLength(line) + 1)
				return line * MaxIndex + GetEndingLength(line);
			return line * MaxIndex + index;
		}

		public int GetPositionIndex(int position, int line)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();
			var linePosition = line * MaxIndex;
			var lineLength = GetLineLength(line);
			var endingPosition = linePosition + lineLength;
			if ((position < linePosition) || (position > endingPosition + GetEndingLength(line)))
				throw new IndexOutOfRangeException();
			if (position > endingPosition)
				return lineLength + 1;
			return position - linePosition;
		}

		public int GetPositionLine(int position)
		{
			if ((position < 0) || (position > MaxPosition))
				throw new IndexOutOfRangeException();
			return position / MaxIndex;
		}
	}
}
