using System;

namespace NeoEdit.Common
{
	public class NERange
	{
		public NERange() : this(0) { }
		public NERange(int pos) : this(pos, pos) { }
		public NERange(int anchor, int cursor)
		{
			Anchor = anchor;
			Cursor = cursor;
			Start = Math.Min(Anchor, Cursor);
			End = Math.Max(Anchor, Cursor);
			Length = End - Start;
			HasSelection = Length != 0;
		}
		public static NERange FromIndex(int index, int length) => new NERange(index, index + length);
		public NERange Move(int offset) => offset == 0 ? this : new NERange(Anchor + offset, Cursor + offset);

		public int Anchor { get; }
		public int Cursor { get; }
		public int Start { get; }
		public int End { get; }
		public int Length { get; }
		public bool HasSelection { get; }

		public override string ToString() => $"({Start:0000000000})->({End:0000000000})";

		public bool Equals(NERange range) => (Start == range.Start) && (End == range.End);
	}
}
