using System;
using System.Collections.Generic;
using System.Text;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program
{
	public class NEText
	{
		public NEText(string text) => this.text = text;

		readonly string text;

		public int Length => text.Length;

		public bool CanEncode(Coder.CodePage codePage) => Coder.CanEncode(text, codePage);

		public byte[] GetBytes(Coder.CodePage codePage) => Coder.StringToBytes(text, codePage, true);

		public char this[int position]
		{
			get
			{
				if ((position < 0) || (position >= text.Length))
					throw new IndexOutOfRangeException();
				return text[position];
			}
		}

		public string GetString() => text;

		public string GetString(Range range) => text.Substring(range.Start, range.Length);

		public string GetString(int start, int length)
		{
			if ((start < 0) || (length < 0) || (start + length > text.Length))
				throw new IndexOutOfRangeException();
			return text.Substring(start, length);
		}

		public NEText Replace(IReadOnlyList<Range> ranges, List<string> text)
		{
			if (ranges.Count != text.Count)
				throw new Exception("Invalid number of arguments");

			int? checkPos = null;
			for (var ctr = 0; ctr < ranges.Count; ctr++)
			{
				if (!checkPos.HasValue)
					checkPos = ranges[ctr].Start;
				if (ranges[ctr].Start < checkPos)
					throw new Exception("Replace data out of order");
				checkPos = ranges[ctr].End;
			}

			var sb = new StringBuilder();
			var dataPos = 0;
			for (var listIndex = 0; listIndex <= text.Count; ++listIndex)
			{
				var position = this.text.Length;
				var length = 0;
				if (listIndex < ranges.Count)
				{
					position = ranges[listIndex].Start;
					length = ranges[listIndex].Length;
				}

				sb.Append(this.text, dataPos, position - dataPos);
				dataPos = position;

				if (listIndex < text.Count)
					sb.Append(text[listIndex]);
				dataPos += length;
			}

			return new NEText(sb.ToString());
		}

		public int IndexOf(char value, int position, int length) => text.IndexOf(value, position, length);
		public int IndexOfAny(char[] anyOf, int position) => text.IndexOfAny(anyOf, position);
		public int IndexOfAny(char[] anyOf, int position, int length) => text.IndexOfAny(anyOf, position, length);
		public bool Equals(NEText neText) => text == neText.text;
	}
}
