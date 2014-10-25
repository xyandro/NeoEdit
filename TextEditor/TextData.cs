using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NeoEdit.Common;
using NeoEdit.Common.Transform;

namespace NeoEdit.TextEditor
{
	// Offsets: absolute positions in data
	// Lines/indexes: positions in data as broken into lines
	// Columns: Positions accounting for tabs
	// Indexes go from 0 - lineLength+1 (columns have equivalent values):
	// 0 is before first character
	// lineLength is after last character
	// lineLength+1 includes line ending
	class TextData
	{
		string _data;
		string data { get { return _data; } set { _data = value; RecalculateLines(); } }
		List<int> lineOffset;
		List<int> lineLength;
		List<int> endingOffset;
		List<int> endingLength;
		public string OnlyEnding { get; private set; }
		public string DefaultEnding { get; private set; }
		const int tabStop = 4;

		public int NumLines { get { return lineOffset.Count; } }
		public int MaxIndex { get; private set; }
		public int MaxColumn { get; private set; }
		public bool BOM { get; private set; }

		public TextData() : this(null, Coder.Type.UTF8) { }
		public TextData(byte[] bytes, Coder.Type encoding)
		{
			if (bytes == null)
				bytes = new byte[0];
			if (encoding == Coder.Type.None)
				throw new Exception("No encoder specified");

			data = Coder.BytesToString(bytes, encoding);
		}

		public byte[] GetBytes(Coder.Type encoding = Coder.Type.UTF8)
		{
			return Coder.StringToBytes(data, encoding);
		}

		void RecalculateLines()
		{
			lineOffset = new List<int>();
			lineLength = new List<int>();
			endingOffset = new List<int>();
			endingLength = new List<int>();

			BOM = (data.Length > 0) && (data[0] == '\ufeff');
			var offset = BOM ? 1 : 0;
			var lineEndChars = new char[] { '\r', '\n' };
			while (offset < data.Length)
			{
				var endLine = data.IndexOfAny(lineEndChars, offset);
				var endLineLen = 1;
				if (endLine == -1)
				{
					endLine = data.Length;
					endLineLen = 0;
				}
				else if ((endLine + 1 < data.Length) && (((data[endLine] == '\n') && (data[endLine + 1] == '\r')) || ((data[endLine] == '\r') && (data[endLine + 1] == '\n'))))
					++endLineLen;
				lineOffset.Add(offset);
				lineLength.Add(endLine - offset);
				endingOffset.Add(endLine);
				endingLength.Add(endLineLen);
				offset = endLine + endLineLen;
			}

			// Always have an ending line
			if ((endingLength.Count == 0) || (endingLength[endingLength.Count - 1] != 0))
			{
				lineOffset.Add(data.Length);
				lineLength.Add(0);
				endingOffset.Add(data.Length);
				endingLength.Add(0);
			}

			// Analyze line endings
			var endingInfo = Enumerable.Range(0, endingOffset.Count).Select(a => GetEnding(a)).Where(a => a.Length != 0).GroupBy(a => a).OrderByDescending(a => a.Count()).Select(a => a.Key).ToList();
			DefaultEnding = endingInfo.FirstOrDefault();
			if (String.IsNullOrEmpty(DefaultEnding))
				DefaultEnding = "\r\n";
			OnlyEnding = endingInfo.Count == 1 ? DefaultEnding : null;

			// Calculate max index/columns
			MaxIndex = lineLength.Max();
			MaxColumn = 0;
			for (var line = 0; line < lineOffset.Count; ++line)
				MaxColumn = Math.Max(MaxColumn, GetLineColumnsLength(line));
		}

		public string this[int line] { get { return GetLine(line); } }
		public char this[int line, int index]
		{
			get
			{
				if ((line < 0) || (line >= lineOffset.Count))
					throw new IndexOutOfRangeException();
				if ((index < 0) || (index >= lineLength[line]))
					throw new IndexOutOfRangeException();
				return data[lineOffset[line] + index];
			}
		}

		public int GetLineLength(int line)
		{
			if ((line < 0) || (line >= lineOffset.Count))
				throw new IndexOutOfRangeException();
			return lineLength[line];
		}

		public int GetLineColumnsLength(int line)
		{
			if ((line < 0) || (line >= lineOffset.Count))
				throw new IndexOutOfRangeException();

			var index = lineOffset[line];
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
			if ((line < 0) || (line >= lineOffset.Count))
				throw new IndexOutOfRangeException();
			return data.Substring(lineOffset[line], lineLength[line]);
		}

		public string GetLineColumns(int line)
		{
			if ((line < 0) || (line >= lineOffset.Count))
				throw new IndexOutOfRangeException();

			var index = lineOffset[line];
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
			if ((line < 0) || (line >= endingOffset.Count))
				throw new IndexOutOfRangeException();
			return data.Substring(endingOffset[line], endingLength[line]);
		}

		public int GetOffset(int line, int index)
		{
			if ((line < 0) || (line >= lineOffset.Count))
				throw new IndexOutOfRangeException();
			if ((index < 0) || (index > lineLength[line] + 1))
				throw new IndexOutOfRangeException();
			if (index == lineLength[line] + 1)
				return endingOffset[line] + endingLength[line];
			return lineOffset[line] + index;
		}

		public int GetOffsetLine(int offset)
		{
			if ((offset < 0) || (offset > data.Length))
				throw new IndexOutOfRangeException();
			var line = lineOffset.BinarySearch(offset);
			if (line < 0)
				line = ~line - 1;
			return line;
		}

		public int GetOffsetIndex(int offset, int line)
		{
			if ((line < 0) || (line >= lineOffset.Count))
				throw new IndexOutOfRangeException();
			if ((offset < lineOffset[line]) || (offset > endingOffset[line] + endingLength[line]))
				throw new IndexOutOfRangeException();
			if (offset > endingOffset[line])
				return lineLength[line] + 1;
			return offset - lineOffset[line];
		}

		public int GetColumnFromIndex(int line, int findIndex)
		{
			if ((line < 0) || (line >= lineOffset.Count))
				throw new IndexOutOfRangeException();
			if ((findIndex < 0) || (findIndex > lineLength[line] + 1))
				throw new IndexOutOfRangeException();

			var column = 0;
			var offset = lineOffset[line];
			var findOffset = findIndex + offset;
			var end = offset + lineLength[line];
			while (offset < findOffset)
			{
				var find = data.IndexOf('\t', offset, end - offset);
				if (find == offset)
				{
					column = (column / tabStop + 1) * tabStop;
					++offset;
					continue;
				}

				if (find == -1)
					find = findOffset - offset;
				else
					find = Math.Min(find, findOffset) - offset;

				column += find;
				offset += find;
			}
			return column;
		}

		public int GetIndexFromColumn(int line, int findColumn, bool returnMaxOnFail = false)
		{
			if ((line < 0) || (line >= lineOffset.Count))
				throw new IndexOutOfRangeException();
			if (findColumn < 0)
				throw new IndexOutOfRangeException();

			var column = 0;
			var offset = lineOffset[line];
			var end = offset + lineLength[line];
			while (column < findColumn)
			{
				var find = data.IndexOf('\t', offset, end - offset);
				if (find == offset)
				{
					column = (column / tabStop + 1) * tabStop;
					++offset;
					continue;
				}

				if (find == -1)
					find = findColumn - column;
				else
					find = Math.Min(find - offset, findColumn - column);

				column += find;
				offset += find;
			}
			if (offset > end + 1)
			{
				if (returnMaxOnFail)
					return lineLength[line] + 1;
				throw new IndexOutOfRangeException();
			}
			return offset - lineOffset[line];
		}

		public string GetString(int start, int length)
		{
			if ((start < 0) || (length < 0) || (start + length > data.Length))
				throw new IndexOutOfRangeException();
			return data.Substring(start, length);
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
				{ '<', '>' },
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

		public List<Tuple<int, int>> RegexMatches(Regex regex, int offset, int length, bool includeEndings, bool regexGroups)
		{
			var result = new List<Tuple<int, int>>();
			var endOffset = offset + length;
			while (offset < endOffset)
			{
				var nextOffset = endOffset;
				if (!includeEndings)
				{
					var line = GetOffsetLine(offset);
					length = Math.Min(lineOffset[line] + lineLength[line], endOffset) - offset;
					nextOffset = Math.Min(endOffset, offset + length + endingLength[line]);
				}

				var matches = regex.Matches(data.Substring(offset, length)).Cast<Match>();
				foreach (var match in matches)
				{
					if ((!regexGroups) || (match.Groups.Count == 1))
						result.Add(new Tuple<int, int>(offset + match.Index, match.Length));
					else
					{
						for (var ctr = 1; ctr < match.Groups.Count; ++ctr)
							if (match.Groups[ctr].Success)
								result.Add(new Tuple<int, int>(offset + match.Groups[ctr].Index, match.Groups[ctr].Length));
					}
				}
				offset = nextOffset;
			}

			return result;
		}

		public List<Tuple<int, int>> StringMatches(Searcher searcher, int offset, int length)
		{
			var result = new List<Tuple<int, int>>();

			var endOffset = offset + length;
			var line = GetOffsetLine(offset);
			var index = GetOffsetIndex(offset, line);
			while (true)
			{
				if (line >= NumLines)
					break;
				var matchOffset = lineOffset[line] + index;
				if (matchOffset >= endOffset)
					break;

				var matchLength = Math.Min(lineOffset[line] + lineLength[line], endOffset) - matchOffset;
				result.AddRange(searcher.Find(data, matchOffset, matchLength));
				++line;
				index = 0;
				continue;
			}
			return result;
		}
	}
}
