using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NeoEdit.Common.Transform;

namespace NeoEdit.TextEditor
{
	class TextData
	{
		string _data;
		string data { get { return _data; } set { _data = value; RecalculateLines(); } }
		List<int> lineIndex;
		List<int> lineLength;
		List<int> endingIndex;
		List<int> endingLength;
		public string DefaultEnding { get; private set; }
		public Coder.Type CoderUsed { get; private set; }
		const int tabStop = 4;

		public int NumLines { get { return lineIndex.Count; } }
		public int MaxIndex { get; private set; }
		public int MaxColumn { get; private set; }
		public bool BOM { get; private set; }

		public TextData(byte[] bytes = null, Coder.Type encoding = Coder.Type.None)
		{
			if (bytes == null)
				bytes = new byte[0];
			if (encoding == Coder.Type.None)
				encoding = Coder.GuessEncoding(bytes);

			data = Coder.BytesToString(bytes, encoding);
			CoderUsed = encoding;
		}

		public byte[] GetBytes(Coder.Type encoding = Coder.Type.UTF8)
		{
			return Coder.StringToBytes(data, encoding);
		}

		void RecalculateLines()
		{
			lineIndex = new List<int>();
			lineLength = new List<int>();
			endingIndex = new List<int>();
			endingLength = new List<int>();

			BOM = (data.Length > 0) && (data[0] == '\ufeff');
			var index = BOM ? 1 : 0;
			var lineEndChars = new char[] { '\r', '\n' };
			while (index < data.Length)
			{
				var endLine = data.IndexOfAny(lineEndChars, index);
				var endLineLen = 1;
				if (endLine == -1)
				{
					endLine = data.Length;
					endLineLen = 0;
				}
				else if ((endLine + 1 < data.Length) && (((data[endLine] == '\n') && (data[endLine + 1] == '\r')) || ((data[endLine] == '\r') && (data[endLine + 1] == '\n'))))
					++endLineLen;
				lineIndex.Add(index);
				lineLength.Add(endLine - index);
				endingIndex.Add(endLine);
				endingLength.Add(endLineLen);
				index = endLine + endLineLen;
			}

			// Always have an ending line
			if ((endingLength.Count == 0) || (endingLength[endingLength.Count - 1] != 0))
			{
				lineIndex.Add(data.Length);
				lineLength.Add(0);
				endingIndex.Add(data.Length);
				endingLength.Add(0);
			}

			// Select most popular line ending
			DefaultEnding = Enumerable.Range(0, endingIndex.Count).Select(a => GetEnding(a)).GroupBy(a => a).OrderByDescending(a => a.Count()).Select(a => a.Key).FirstOrDefault();
			if (String.IsNullOrEmpty(DefaultEnding))
				DefaultEnding = "\r\n";

			// Calculate max index/columns
			MaxIndex = lineLength.Max();
			MaxColumn = 0;
			for (var line = 0; line < lineIndex.Count; ++line)
				MaxColumn = Math.Max(MaxColumn, GetColumnFromIndex(line, GetLineColumnsLength(line)));
		}

		public string this[int line] { get { return GetLine(line); } }

		public int GetLineLength(int line)
		{
			if ((line < 0) || (line >= lineIndex.Count))
				throw new IndexOutOfRangeException();
			return lineLength[line];
		}

		public int GetLineColumnsLength(int line)
		{
			if ((line < 0) || (line >= lineIndex.Count))
				throw new IndexOutOfRangeException();

			var index = lineIndex[line];
			var len = lineLength[line];
			var columns = 0;
			while (len > 0)
			{
				var find = data.IndexOf('\t', index, len);
				if (find == index)
				{
					columns = (columns / tabStop + 1) * tabStop;
					++index;
					--len;
					continue;
				}

				if (find == -1)
					find = len;
				else
					find -= index;
				columns += find;
				index += find;
				len -= find;
			}

			return columns;
		}

		public string GetLine(int line)
		{
			if ((line < 0) || (line >= lineIndex.Count))
				throw new IndexOutOfRangeException();
			return data.Substring(lineIndex[line], lineLength[line]);
		}

		public string GetColumnsLine(int line)
		{
			if ((line < 0) || (line >= lineIndex.Count))
				throw new IndexOutOfRangeException();

			var index = lineIndex[line];
			var len = lineLength[line];
			var sb = new StringBuilder();
			while (len > 0)
			{
				var find = data.IndexOf('\t', index, len);
				if (find == index)
				{
					sb.Append(' ', (sb.Length / tabStop + 1) * tabStop - sb.Length);
					++index;
					--len;
					continue;
				}

				if (find == -1)
					find = len;
				else
					find -= index;
				sb.Append(data, index, find);
				index += find;
				len -= find;
			}

			return sb.ToString();
		}

		public string GetEnding(int line)
		{
			if ((line < 0) || (line >= endingIndex.Count))
				throw new IndexOutOfRangeException();
			return data.Substring(endingIndex[line], endingLength[line]);
		}

		public int GetOffset(int line, int index)
		{
			if ((line < 0) || (line >= endingIndex.Count))
				throw new IndexOutOfRangeException();
			return lineIndex[line] + index;
		}

		public int GetOffsetLine(int offset)
		{
			var line = lineIndex.BinarySearch(offset);
			if (line < 0)
				line = ~line - 1;
			return line;
		}

		public int GetOffsetIndex(int offset, int line)
		{
			return offset - lineIndex[line];
		}

		public int GetColumnFromIndex(int line, int index)
		{
			if (index < 0)
				throw new IndexOutOfRangeException();

			var column = 0;
			var tmpIndex = lineIndex[line];
			var len = lineLength[line];
			while (index > 0)
			{
				var find = data.IndexOf('\t', tmpIndex, len);
				if (find == tmpIndex)
				{
					column = (column / tabStop + 1) * tabStop;
					++tmpIndex;
					--len;
					--index;
					continue;
				}

				if (find == -1)
					find = index;
				else
					find = Math.Min(find - tmpIndex, index);

				column += find;
				tmpIndex += find;
				len -= find;
				index -= find;
			}
			return column;
		}

		public int GetIndexFromColumn(int line, int column)
		{
			if (column < 0)
				throw new IndexOutOfRangeException();

			var tmpColumn = 0;
			var index = 0;
			var tmpIndex = lineIndex[line];
			var len = lineLength[line];
			while (tmpColumn < column)
			{
				var find = data.IndexOf('\t', tmpIndex, len);
				if (find == tmpIndex)
				{
					tmpColumn = (tmpColumn / tabStop + 1) * tabStop;
					++tmpIndex;
					--len;
					++index;
					continue;
				}

				if (find == -1)
					find = column - tmpColumn;
				else
					find = Math.Min(find - tmpIndex, column - tmpColumn);

				tmpColumn += find;
				tmpIndex += find;
				len -= find;
				index += find;
			}
			return index;
		}

		public string GetString(int start, int end)
		{
			return data.Substring(start, end - start);
		}

		public void Replace(List<int> offsets, List<int> lengths, List<string> text)
		{
			if ((offsets.Count != lengths.Count) || (offsets.Count != text.Count))
				throw new Exception("Invalid number of arguments");

			int? checkPos = null;
			for (var ctr = 0; ctr < offsets.Count; ctr++)
			{
				if (!checkPos.HasValue)
					checkPos = offsets[ctr];
				if (offsets[ctr] < checkPos)
					throw new Exception("Replace data out of order");
				checkPos = offsets[ctr] + lengths[ctr];
			}

			var sb = new StringBuilder();
			var dataPos = 0;
			for (var listIndex = 0; listIndex <= text.Count; ++listIndex)
			{
				var offset = data.Length;
				var length = 0;
				if (listIndex < offsets.Count)
				{
					offset = offsets[listIndex];
					length = lengths[listIndex];
				}

				sb.Append(data, dataPos, offset - dataPos);
				dataPos = offset;

				if (listIndex < text.Count)
					sb.Append(text[listIndex]);
				dataPos += length;
			}

			data = sb.ToString();
		}

		public int GetOppositeBracket(int offset)
		{
			if ((offset < 0) || (offset > data.Length))
				return -1;

			var dict = new Dictionary<char, char>
			{
				{ '(', ')' },
				{ '{', '}' },
				{ '[', ']' },
			};

			var found = default(KeyValuePair<char, char>);
			if ((found.Key == 0) && (offset < data.Length))
				found = dict.FirstOrDefault(entry => (entry.Key == data[offset]) || (entry.Value == data[offset]));
			if (found.Key == 0)
			{
				if (--offset < 0)
					return -1;
				found = dict.FirstOrDefault(entry => (entry.Key == data[offset]) || (entry.Value == data[offset]));
			}
			if (found.Key == 0)
				return -1;

			var direction = found.Key == data[offset] ? 1 : -1;

			var num = 0;
			for (; offset < data.Length; offset += direction)
			{
				if (data[offset] == found.Key)
					++num;
				if (data[offset] == found.Value)
					--num;

				if (num == 0)
					return offset + Math.Max(0, direction);
			}

			return -1;
		}
	}
}
