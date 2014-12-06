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
		public static Range FromIndex(int index, int length) { return new Range(index + length, index); }

		public int Cursor { get; private set; }
		public int Highlight { get; private set; }
		public int Start { get; private set; }
		public int End { get; private set; }
		public int Length { get; private set; }
		public bool HasSelection { get; private set; }

		public override string ToString()
		{
			return String.Format("({0:0000000000})->({1:0000000000})", Start, End);
		}
	}
}
