using System;

namespace NeoEdit.Common
{
	public class Range
	{
		public Range() : this(0) { }
		public Range(int pos) : this(pos, pos) { }
		public Range(int anchor, int cursor)
		{
			Anchor = anchor;
			Cursor = cursor;
			Start = Math.Min(Anchor, Cursor);
			End = Math.Max(Anchor, Cursor);
			Length = End - Start;
			HasSelection = Length != 0;
		}
		public static Range FromIndex(int index, int length) => new Range(index, index + length);
		public Range Move(int offset) => offset == 0 ? this : new Range(Anchor + offset, Cursor + offset);

		public int Anchor { get; }
		public int Cursor { get; }
		public int Start { get; }
		public int End { get; }
		public int Length { get; }
		public bool HasSelection { get; }

		public override string ToString() => $"({Start:0000000000})->({End:0000000000})";

		public bool Equals(Range range) => (Start == range.Start) && (End == range.End);
	}
}
