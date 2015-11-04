using System;

namespace NeoEdit.TextEdit
{
	class Range
	{
		public Range() : this(0) { }
		public Range(int pos) : this(pos, pos) { }
		public Range(int cursor, int highlight)
		{
			Cursor = cursor;
			Highlight = highlight;
			Start = Math.Min(Cursor, Highlight);
			End = Math.Max(Cursor, Highlight);
			Length = End - Start;
			HasSelection = Length != 0;
		}
		public static Range FromIndex(int index, int length) => new Range(index + length, index);

		public int Cursor { get; }
		public int Highlight { get; }
		public int Start { get; }
		public int End { get; }
		public int Length { get; }
		public bool HasSelection { get; }

		public override string ToString() => $"({Start:0000000000})->({End:0000000000})";
	}
}
