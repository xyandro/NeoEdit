using System;

namespace NeoEdit.TextEdit
{
	class Range
	{
		public Range() : this(0) { }
		public Range(int pos) : this(pos, pos) { }
		public Range(int cursor, int anchor)
		{
			Cursor = cursor;
			Anchor = anchor;
			Start = Math.Min(Cursor, Anchor);
			End = Math.Max(Cursor, Anchor);
			Length = End - Start;
			HasSelection = Length != 0;
		}
		public static Range FromIndex(int index, int length) => new Range(index + length, index);

		public int Cursor { get; }
		public int Anchor { get; }
		public int Start { get; }
		public int End { get; }
		public int Length { get; }
		public bool HasSelection { get; }

		public override string ToString() => $"({Start:0000000000})->({End:0000000000})";
	}
}
