using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NeoEdit.Common.Transform;

namespace NeoEdit.Common
{
	// Offsets: absolute positions in data
	// Lines/indexes: positions in data as broken into lines
	// Columns: Positions accounting for tabs
	// Indexes go from 0 - lineLength+1 (columns have equivalent values):
	// 0 is before first character
	// lineLength is after last character
	// lineLength+1 includes line ending
	public class TextData
	{
		string _data;
		public string Data { get { return _data; } private set { _data = value; RecalculateLines(); } }
		List<int> lineOffset;
		List<int> endingOffset;
		public string OnlyEnding { get; private set; }
		public string DefaultEnding { get; private set; }
		const int tabStop = 4;

		public int NumLines { get { return endingOffset.Count; } }
		public int NumChars { get { return Data.Length; } }
		public int MaxIndex { get; private set; }
		public int MaxColumn { get; private set; }

		public TextData() : this(null, Coder.CodePage.UTF8) { }
		public TextData(byte[] bytes, Coder.CodePage codePage)
		{
			if (bytes == null)
				bytes = new byte[0];
			if (codePage == Coder.CodePage.None)
				throw new Exception("No encoder specified");

			Data = Coder.BytesToString(bytes, codePage, true);
		}

		public bool CanFullyEncode(Coder.CodePage codePage)
		{
			return Coder.CanFullyEncode(Data, codePage);
		}

		public byte[] GetBytes(Coder.CodePage codePage)
		{
			return Coder.StringToBytes(Data, codePage, true);
		}

		void RecalculateLines()
		{
			const int Ending_None = 0;
			const int Ending_CR = 1;
			const int Ending_LF = 2;
			const int Ending_CRLF = 3;
			const int Ending_Mixed = 4;

			lineOffset = new List<int>();
			endingOffset = new List<int>();
			MaxIndex = MaxColumn = 0;

			var lineEndChars = new char[] { '\r', '\n' };

			var chunkSize = Math.Max(65536, Data.Length / 32);
			var startChunk = 0;
			var chunks = new List<Tuple<int, int>>();
			while (startChunk < Data.Length)
			{
				var endChunk = Data.IndexOfAny(lineEndChars, Math.Min(Data.Length, startChunk + chunkSize));
				if (endChunk == -1)
					endChunk = Data.Length;
				while ((endChunk < Data.Length) && (lineEndChars.Contains(Data[endChunk])))
					++endChunk;

				chunks.Add(Tuple.Create(startChunk, endChunk));

				startChunk = endChunk;
			}

			var chunkLineOffsets = chunks.Select(chunk => new List<int>()).ToList();
			var chunkEndingOffsets = chunks.Select(chunk => new List<int>()).ToList();

			int defaultEnding = Ending_None, onlyEnding = Ending_None;
			Parallel.ForEach(chunks, chunk =>
			{
				var index = chunks.IndexOf(chunk);
				int chunkDefaultEnding = Ending_None, chunkOnlyEnding = Ending_None;
				var chunkLineOffset = chunkLineOffsets[index];
				var chunkEndingOffset = chunkEndingOffsets[index];
				var chunkMaxIndex = 0;

				var offset = chunk.Item1;
				while (offset < chunk.Item2)
				{
					var endLine = Data.IndexOfAny(lineEndChars, offset, chunk.Item2 - offset);
					var endLineLen = 1;
					var ending = Ending_None;
					if (endLine == -1)
					{
						endLine = chunk.Item2;
						endLineLen = 0;
					}
					else if ((endLine + 1 < chunk.Item2) && (Data[endLine] == '\r') && (Data[endLine + 1] == '\n'))
					{
						++endLineLen;
						ending = Ending_CRLF;
					}
					else
						ending = Data[endLine] == '\r' ? Ending_CR : Ending_LF;

					if (ending != Ending_None)
					{
						if (chunkDefaultEnding == Ending_None)
							chunkDefaultEnding = ending;
						if (chunkOnlyEnding == Ending_None)
							chunkOnlyEnding = ending;
						if (chunkOnlyEnding != ending)
							chunkOnlyEnding = Ending_Mixed;
					}
					chunkLineOffset.Add(offset);
					chunkEndingOffset.Add(endLine);
					offset = endLine + endLineLen;
					chunkMaxIndex = Math.Max(chunkMaxIndex, endLine - offset);
				}

				lock (chunkLineOffsets)
				{
					if (defaultEnding == Ending_None)
						defaultEnding = chunkDefaultEnding;
					if (onlyEnding == Ending_None)
						onlyEnding = chunkOnlyEnding;
					if (onlyEnding != chunkOnlyEnding)
						onlyEnding = Ending_Mixed;
					MaxIndex = Math.Max(MaxIndex, chunkMaxIndex);
				}
			});

			chunkLineOffsets.ForEach(values => lineOffset.AddRange(values));
			chunkEndingOffsets.ForEach(values => endingOffset.AddRange(values));

			// Always have an ending line
			if ((endingOffset.Count == 0) || (endingOffset.Last() != Data.Length))
			{
				lineOffset.Add(Data.Length);
				endingOffset.Add(Data.Length);
			}

			// Used only for calculating length
			lineOffset.Add(Data.Length);

			var endingText = new Dictionary<int, string>
			{
				{ Ending_None, "\r\n" },
				{ Ending_CR, "\r" },
				{ Ending_LF, "\n" },
				{ Ending_CRLF, "\r\n" },
				{ Ending_Mixed, null },
			};

			DefaultEnding = endingText[defaultEnding];
			OnlyEnding = endingText[onlyEnding];

			// Calculate max index/columns
			MaxColumn = Enumerable.Range(0, NumLines).AsParallel().Max(line => GetLineColumnsLength(line));
		}

		public string this[int line] { get { return GetLine(line); } }
		public char this[int line, int index]
		{
			get
			{
				if ((line < 0) || (line >= NumLines))
					throw new IndexOutOfRangeException();
				if ((index < 0) || (index >= GetLineLength(line)))
					throw new IndexOutOfRangeException();
				return Data[lineOffset[line] + index];
			}
		}

		public int GetLineLength(int line)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();
			return endingOffset[line] - lineOffset[line];
		}

		public int GetEndingLength(int line)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();
			return lineOffset[line + 1] - endingOffset[line];
		}

		public int GetLineColumnsLength(int line)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();

			var index = lineOffset[line];
			var len = GetLineLength(line);
			var columns = 0;
			while (len > 0)
			{
				var find = Data.IndexOf('\t', index, len);
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
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();
			return Data.Substring(lineOffset[line], GetLineLength(line));
		}

		public string GetLineColumns(int line)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();

			var index = lineOffset[line];
			var len = GetLineLength(line);
			var sb = new StringBuilder();
			while (len > 0)
			{
				var find = Data.IndexOf('\t', index, len);
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
				sb.Append(Data, index, find);
				index += find;
				len -= find;
			}

			return sb.ToString();
		}

		public string GetEnding(int line)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();
			return Data.Substring(endingOffset[line], GetEndingLength(line));
		}

		public int GetOffset(int line, int index)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();
			if ((index < 0) || (index > GetLineLength(line) + 1))
				throw new IndexOutOfRangeException();
			if (index == GetLineLength(line) + 1)
				return endingOffset[line] + GetEndingLength(line);
			return lineOffset[line] + index;
		}

		public int GetOffsetLine(int offset)
		{
			if ((offset < 0) || (offset > Data.Length))
				throw new IndexOutOfRangeException();
			var line = lineOffset.BinarySearch(offset);
			if (line < 0)
				line = ~line - 1;
			if (line == lineOffset.Count - 1)
				--line;
			return line;
		}

		public int GetOffsetIndex(int offset, int line)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();
			if ((offset < lineOffset[line]) || (offset > endingOffset[line] + GetEndingLength(line)))
				throw new IndexOutOfRangeException();
			if (offset > endingOffset[line])
				return GetLineLength(line) + 1;
			return offset - lineOffset[line];
		}

		public int GetColumnFromIndex(int line, int findIndex)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();
			if ((findIndex < 0) || (findIndex > GetLineLength(line) + 1))
				throw new IndexOutOfRangeException();

			var column = 0;
			var offset = lineOffset[line];
			var findOffset = findIndex + offset;
			var end = offset + GetLineLength(line);
			while (offset < findOffset)
			{
				var find = Data.IndexOf('\t', offset, end - offset);
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
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();
			if (findColumn < 0)
				throw new IndexOutOfRangeException();

			var column = 0;
			var offset = lineOffset[line];
			var end = offset + GetLineLength(line);
			while (column < findColumn)
			{
				var find = Data.IndexOf('\t', offset, end - offset);
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
					return GetLineLength(line) + 1;
				throw new IndexOutOfRangeException();
			}
			return offset - lineOffset[line];
		}

		public string GetString(int start, int length)
		{
			if ((start < 0) || (length < 0) || (start + length > Data.Length))
				throw new IndexOutOfRangeException();
			return Data.Substring(start, length);
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
				var offset = Data.Length;
				var length = 0;
				if (listIndex < offsets.Count)
				{
					offset = offsets[listIndex];
					length = lengths[listIndex];
				}

				sb.Append(Data, dataPos, offset - dataPos);
				dataPos = offset;

				if (listIndex < text.Count)
					sb.Append(text[listIndex]);
				dataPos += length;
			}

			Data = sb.ToString();
		}

		public int GetOppositeBracket(int offset)
		{
			if ((offset < 0) || (offset > Data.Length))
				return -1;

			var dict = new Dictionary<char, char>
			{
				{ '(', ')' },
				{ '{', '}' },
				{ '[', ']' },
				{ '<', '>' },
			};

			var found = default(KeyValuePair<char, char>);
			if ((found.Key == 0) && (offset < Data.Length))
				found = dict.FirstOrDefault(entry => (entry.Key == Data[offset]) || (entry.Value == Data[offset]));
			if (found.Key == 0)
			{
				if (--offset < 0)
					return -1;
				found = dict.FirstOrDefault(entry => (entry.Key == Data[offset]) || (entry.Value == Data[offset]));
			}
			if (found.Key == 0)
				return -1;

			var direction = found.Key == Data[offset] ? 1 : -1;

			var num = 0;
			for (; offset < Data.Length; offset += direction)
			{
				if (Data[offset] == found.Key)
					++num;
				if (Data[offset] == found.Value)
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
					length = Math.Min(lineOffset[line] + GetLineLength(line), endOffset) - offset;
					nextOffset = Math.Min(endOffset, offset + length + GetEndingLength(line));
				}

				var matches = regex.Matches(Data.Substring(offset, length)).Cast<Match>();
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

				var matchLength = Math.Min(lineOffset[line] + GetLineLength(line), endOffset) - matchOffset;
				result.AddRange(searcher.Find(Data, matchOffset, matchLength));
				++line;
				index = 0;
				continue;
			}
			return result;
		}

		public void Trim(ref int offset, ref int length)
		{
			while ((length > 0) && (Char.IsWhiteSpace(Data[offset + length - 1])))
				--length;
			while ((length > 0) && (Char.IsWhiteSpace(Data[offset])))
			{
				++offset;
				--length;
			}
		}
	}
}
