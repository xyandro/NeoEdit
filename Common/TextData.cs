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

		public int NumLines => endingOffset.Count;
		public int NumChars => Data.Length;
		public int MaxIndex { get; private set; }
		public int MaxColumn { get; private set; }

		public TextData(string data = "")
		{
			Data = data;
		}

		public bool CanEncode(Coder.CodePage codePage) => Coder.CanEncode(Data, codePage);

		public byte[] GetBytes(Coder.CodePage codePage) => Coder.StringToBytes(Data, codePage, true);

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
				[Ending_None] = "\r\n",
				[Ending_CR] = "\r",
				[Ending_LF] = "\n",
				[Ending_CRLF] = "\r\n",
				[Ending_Mixed] = null,
			};

			DefaultEnding = endingText[defaultEnding];
			OnlyEnding = endingText[onlyEnding];

			// Calculate max index/columns
			MaxColumn = Enumerable.Range(0, NumLines).AsParallel().Max(line => GetLineColumnsLength(line));

			diffData = null;
		}

		public string this[int line] => GetLine(line);
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

		public string GetLine(int line, bool includeEnding = false)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();
			return Data.Substring(lineOffset[line], GetLineLength(line) + (includeEnding ? GetEndingLength(line) : 0));
		}

		public string GetLineColumns(int line, bool includeEnding = false)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();

			var index = lineOffset[line];
			var len = GetLineLength(line) + (includeEnding ? GetEndingLength(line) : 0);
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

		public List<int> GetLineColumnMap(int line, bool includeEnding = false)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();

			var result = new List<int>();
			var index = lineOffset[line];
			var len = GetLineLength(line) + (includeEnding ? GetEndingLength(line) : 0);
			var outPos = 0;
			while (len > 0)
			{
				result.Add(outPos);
				if (Data[index] == '\t')
					outPos = (outPos / tabStop + 1) * tabStop;
				else
					++outPos;
				++index;
				--len;
			}
			result.Add(outPos);

			return result;
		}

		public string GetEnding(int line)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();
			return Data.Substring(endingOffset[line], GetEndingLength(line));
		}

		public int GetOffset(int line, int index, bool allowJustPastEnd = false)
		{
			if ((allowJustPastEnd) && (line == NumLines) && (index == 0))
				return Data.Length;

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
			while ((line < lineOffset.Count - 2) && (lineOffset[line] == lineOffset[line + 1]))
				++line;
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

		public List<Tuple<int, int>> RegexMatches(Regex regex, int offset, int length, bool multiLine, bool regexGroups, bool firstOnly)
		{
			var result = new List<Tuple<int, int>>();
			var endOffset = offset + length;
			while (offset < endOffset)
			{
				var nextOffset = endOffset;
				if (!multiLine)
				{
					var line = GetOffsetLine(offset);
					length = Math.Max(0, Math.Min(lineOffset[line] + GetLineLength(line), endOffset) - offset); // Could have been negative if selection encompasses half of \r\n line break
					nextOffset = Math.Min(endOffset, lineOffset[line + 1]);
				}

				var matches = regex.Matches(Data.Substring(offset, length)).Cast<Match>();
				foreach (var match in matches)
				{
					if ((!regexGroups) || (match.Groups.Count == 1))
						result.Add(new Tuple<int, int>(offset + match.Index, match.Length));
					else
						result.AddRange(match.Groups.Cast<Group>().Skip(1).Where(group => group.Success).SelectMany(group => group.Captures.Cast<Capture>()).Select(capture => new Tuple<int, int>(offset + capture.Index, capture.Length)));
					if ((firstOnly) && (result.Count != 0))
						return result;
				}
				offset = nextOffset;
			}

			return result;
		}

		public List<Tuple<int, int>> StringMatches(Searcher searcher, int offset, int length)
		{
			return searcher.Find(Data, offset, length);
		}

		public void Trim(ref int offset, ref int length, HashSet<char> chars, bool start, bool end)
		{
			if (end)
			{
				while ((length > 0) && (chars.Contains(Data[offset + length - 1])))
					--length;
			}
			if (start)
			{
				while ((length > 0) && (chars.Contains(Data[offset])))
				{
					++offset;
					--length;
				}
			}
		}

		class DiffParams
		{
			public readonly bool IgnoreWhitespace, IgnoreCase, IgnoreNumbers, IgnoreLineEndings;
			public readonly string IgnoreCharacters;
			public readonly int LineStartTabStop;

			readonly HashSet<char> IgnoreCharactersHash;

			public DiffParams(bool ignoreWhitespace, bool ignoreCase, bool ignoreNumbers, bool ignoreLineEndings, string ignoreCharacters, int lineStartTabStop = 0)
			{
				IgnoreWhitespace = ignoreWhitespace;
				IgnoreCase = ignoreCase;
				IgnoreNumbers = ignoreNumbers;
				IgnoreLineEndings = ignoreLineEndings;
				IgnoreCharacters = ignoreCharacters;
				LineStartTabStop = lineStartTabStop;

				IgnoreCharactersHash = new HashSet<char>(IgnoreCharacters ?? "");
			}

			public Tuple<string, List<int>> FormatLine(string line)
			{
				var map = new List<int>();
				var sb = new StringBuilder(line.Length);
				var inNumber = false;
				var lineStart = LineStartTabStop != 0;
				for (var ctr = 0; ctr < line.Length; ++ctr)
				{
					var ch = line[ctr];

					if ((lineStart) && ((!char.IsWhiteSpace(ch)) || (ch == '\r') || (ch == '\n')))
						lineStart = false;
					if (lineStart)
					{
						var str = ch.ToString();
						if (str == "\t")
							str = new string(' ', LineStartTabStop - sb.Length % LineStartTabStop);
						map.Add(ctr);
						sb.Append(str);
						continue;
					}

					var isNumber = (char.IsDigit(ch)) || ((ch == '.') && (((ctr - 1 >= 0) && (char.IsDigit(line[ctr - 1]))) || ((ctr + 1 < line.Length) && (char.IsDigit(line[ctr + 1])))));
					if (!isNumber)
						inNumber = false;
					if (IgnoreCharactersHash.Contains(ch))
						continue;
					if ((IgnoreWhitespace) && (char.IsWhiteSpace(ch)) && (ch != '\r') && (ch != '\n'))
						continue;
					if (IgnoreCase)
						ch = char.ToLowerInvariant(ch);
					if ((IgnoreNumbers) && (isNumber))
					{
						if (inNumber)
							continue;
						inNumber = true;
						ch = '0';
					}
					if ((IgnoreLineEndings) && ((ch == '\r') || (ch == '\n')))
						continue;

					map.Add(ctr);
					sb.Append(ch);
				}
				map.Add(line.Length);

				return Tuple.Create(sb.ToString(), map);
			}

			public bool Equals(DiffParams diffParams)
			{
				if (IgnoreWhitespace != diffParams.IgnoreWhitespace)
					return false;
				if (IgnoreCase != diffParams.IgnoreCase)
					return false;
				if (IgnoreNumbers != diffParams.IgnoreNumbers)
					return false;
				if (IgnoreLineEndings != diffParams.IgnoreLineEndings)
					return false;
				if (IgnoreCharacters != diffParams.IgnoreCharacters)
					return false;
				return true;
			}
		}

		class DiffData
		{
			public string Data;
			public DiffParams DiffParams;
			public List<LCS.MatchType> LineCompare;
			public List<Tuple<int, int>>[] ColCompare;
			public Dictionary<int, int> LineMap, LineRevMap;

			public DiffData(string data, DiffParams diffParams)
			{
				Data = data;
				DiffParams = diffParams;
			}
		}
		DiffData diffData;

		public static void CalculateDiff(TextData textData0, TextData textData1, bool ignoreWhitespace, bool ignoreCase, bool ignoreNumbers, bool ignoreLineEndings, string ignoreCharacters)
		{
			var diffParams = new DiffParams(ignoreWhitespace, ignoreCase, ignoreNumbers, ignoreLineEndings, ignoreCharacters);
			if ((textData0.diffData != null) && (textData1.diffData != null) && (textData0.diffData.Data == textData0.Data) && (textData1.diffData.Data == textData1.Data) && (textData0.diffData.DiffParams.Equals(diffParams)) && (textData1.diffData.DiffParams.Equals(diffParams)))
				return;

			var textData = new TextData[] { textData0, textData1 };
			var lines = new List<string>[2];
			var map = new List<List<int>>[2];
			for (var pass = 0; pass < 2; ++pass)
			{
				textData[pass].ClearDiff();
				textData[pass].diffData = new DiffData(textData[pass].Data, diffParams);
				var formatDiffLine = Enumerable.Range(0, textData[pass].NumLines).Select(line => diffParams.FormatLine(textData[pass].GetLine(line, true))).ToList();
				lines[pass] = formatDiffLine.Select(val => val.Item1).ToList();
				map[pass] = formatDiffLine.Select(val => val.Item2).ToList();
			}

			var linesLCS = LCS.GetLCS(lines[0], lines[1], str => !string.IsNullOrWhiteSpace(str));

			for (var pass = 0; pass < 2; ++pass)
			{
				textData[pass].diffData.LineCompare = linesLCS.Select(val => val[pass]).ToList();
				for (var ctr = 0; ctr < linesLCS.Count; ++ctr)
					if (linesLCS[ctr][pass] == LCS.MatchType.Gap)
					{
						textData[pass].lineOffset.Insert(ctr, textData[pass].lineOffset[ctr]);
						textData[pass].endingOffset.Insert(ctr, textData[pass].lineOffset[ctr]);
					}

				textData[pass].diffData.LineMap = new Dictionary<int, int>();
				var pos = -1;
				for (var line = 0; line < linesLCS.Count; ++line)
				{
					if (linesLCS[line][pass] != LCS.MatchType.Gap)
						++pos;
					textData[pass].diffData.LineMap[line] = pos;
				}
				textData[pass].diffData.LineRevMap = textData[pass].diffData.LineMap.GroupBy(pair => pair.Value).ToDictionary(group => group.Key, group => group.Min(pair => pair.Key));
				textData[pass].diffData.ColCompare = new List<Tuple<int, int>>[linesLCS.Count];
			}

			var curLine = new int[] { -1, -1 };
			for (var line = 0; line < textData0.diffData.ColCompare.Length; ++line)
			{
				for (var pass = 0; pass < 2; ++pass)
					++curLine[pass];

				if (linesLCS[line].IsMatch)
					continue;

				var skip = false;
				for (var pass = 0; pass < 2; ++pass)
				{
					if (linesLCS[line][pass] == LCS.MatchType.Gap)
					{
						--curLine[pass];
						textData[1 - pass].diffData.ColCompare[line] = new List<Tuple<int, int>> { Tuple.Create(0, int.MaxValue) };
						skip = true;
					}
				}
				if (skip)
					continue;

				var colsLCS = LCS.GetLCS(lines[0][curLine[0]], lines[1][curLine[1]], ch => char.IsLetterOrDigit(ch));

				for (var pass = 0; pass < 2; ++pass)
				{
					var start = default(int?);
					var pos = -1;
					textData[pass].diffData.ColCompare[line] = new List<Tuple<int, int>>();
					for (var ctr = 0; ctr <= colsLCS.Count; ++ctr)
					{
						if ((ctr == colsLCS.Count) || (colsLCS[ctr][pass] != LCS.MatchType.Gap))
							++pos;

						if ((ctr == colsLCS.Count) || (colsLCS[ctr].IsMatch))
						{
							if (start.HasValue)
								textData[pass].diffData.ColCompare[line].Add(Tuple.Create(map[pass][curLine[pass]][start.Value], map[pass][curLine[pass]][pos]));
							start = null;
							continue;
						}

						if (colsLCS[ctr][pass] == LCS.MatchType.Mismatch)
							start = start ?? pos;
					}
				}
			}
		}

		public void ClearDiff()
		{
			if (diffData == null)
				return;

			for (var line = diffData.LineCompare.Count - 1; line >= 0; --line)
				if (diffData.LineCompare[line] == LCS.MatchType.Gap)
				{
					lineOffset.RemoveAt(line);
					endingOffset.RemoveAt(line);
				}

			diffData = null;
		}

		static public Tuple<List<Tuple<int, int>>, List<string>> GetDiffFixes(TextData src, TextData dest, int lineStartTabStop, bool? ignoreWhitespace, bool? ignoreCase, bool? ignoreNumbers, bool? ignoreLineEndings, string ignoreCharacters)
		{
			var textData = new TextData[] { src, dest };
			var lineMap = new Dictionary<int, int>[2];
			var lines = new List<string>[2];
			var textLines = new List<string>[2];
			var diffParams = new DiffParams(ignoreWhitespace ?? true, ignoreCase ?? true, ignoreNumbers ?? true, ignoreLineEndings ?? true, ignoreCharacters, lineStartTabStop);
			for (var pass = 0; pass < 2; ++pass)
			{
				lineMap[pass] = Enumerable.Range(0, textData[pass].NumLines).Indexes(line => textData[pass].diffData?.LineCompare[line] != LCS.MatchType.Gap).Select((index1, index2) => new { index1, index2 }).ToDictionary(obj => obj.index2, obj => obj.index1);
				lines[pass] = lineMap[pass].Values.Select(line => textData[pass].GetLine(line, true)).ToList();
				textLines[pass] = lines[pass].Select(line => diffParams.FormatLine(line).Item1).ToList();
			}

			var linesLCS = LCS.GetLCS(textLines[0], textLines[1], str => !string.IsNullOrWhiteSpace(str));

			var ranges = new List<Tuple<int, int>>();
			var strs = new List<string>();
			var curLine = new int[] { -1, -1 };
			diffParams = new DiffParams(ignoreWhitespace ?? false, ignoreCase ?? false, ignoreNumbers ?? false, ignoreLineEndings ?? src.OnlyEnding != null, ignoreCharacters);
			for (var line = 0; line < linesLCS.Count; ++line)
			{
				var mappedCurLine = new int[2];
				for (var pass = 0; pass < 2; ++pass)
					if (linesLCS[line][pass] != LCS.MatchType.Gap)
					{
						++curLine[pass];
						mappedCurLine[pass] = lineMap[pass][curLine[pass]];
					}

				if (linesLCS[line].IsMatch)
				{
					var colLines = new string[2];
					var map = new List<int>[2];
					for (var pass = 0; pass < 2; ++pass)
					{
						var formatDiffLine = diffParams.FormatLine(lines[pass][curLine[pass]]);
						colLines[pass] = formatDiffLine.Item1;
						map[pass] = formatDiffLine.Item2;
					}

					if (colLines[0] != colLines[1])
					{
						var colsLCS = LCS.GetLCS(colLines[0], colLines[1]);
						for (var pass = 0; pass < 2; ++pass)
						{
							var start = default(int?);
							var pos = -1;
							for (var ctr = 0; ctr <= colsLCS.Count; ++ctr)
							{
								if ((ctr == colsLCS.Count) || (colsLCS[ctr][pass] != LCS.MatchType.Gap))
									++pos;

								if ((ctr == colsLCS.Count) || (colsLCS[ctr].IsMatch))
								{
									if (start.HasValue)
									{
										var lineOffset = textData[pass].GetOffset(mappedCurLine[pass], 0);
										var begin = lineOffset + map[pass][start.Value];
										var end = lineOffset + map[pass][pos];
										if (pass == 0)
											strs.Add(textData[pass].GetString(begin, end - begin));
										else
											ranges.Add(Tuple.Create(begin, end));
										start = null;
									}
									continue;
								}

								start = start ?? pos + (colsLCS[ctr][pass] == LCS.MatchType.Gap ? 1 : 0);
							}
						}
					}
				}

				if ((ignoreLineEndings == null) && (src.OnlyEnding != null) && (linesLCS[line][1] != LCS.MatchType.Gap))
				{
					var endingStart = dest.endingOffset[mappedCurLine[1]];
					var endingEnd = dest.lineOffset[mappedCurLine[1] + 1];
					if (endingStart == endingEnd)
						continue;

					if (dest.Data.Substring(endingStart, endingEnd - endingStart) != src.OnlyEnding)
					{
						ranges.Add(Tuple.Create(endingStart, endingEnd));
						strs.Add(src.OnlyEnding);
					}
				}
			}

			return Tuple.Create(ranges, strs);
		}

		public bool IsDiffGapLine(int line) => diffData == null ? false : diffData.LineCompare[line] == LCS.MatchType.Gap;
		public int GetDiffLine(int line) => (diffData == null) || (line >= diffData.LineMap.Count) ? line : diffData.LineMap[line];
		public int GetNonDiffLine(int line) => (diffData == null) || (line >= diffData.LineRevMap.Count) ? line : diffData.LineRevMap[line];
		public bool GetLineDiffMatches(int line) => diffData == null ? true : diffData.LineCompare[line] == LCS.MatchType.Match;
		public List<Tuple<int, int>> GetLineColumnDiffs(int line) => diffData?.ColCompare[line] ?? new List<Tuple<int, int>>();

		public int SkipDiffGaps(int line, int direction)
		{
			while ((diffData != null) && (line >= 0) && (line < diffData.LineCompare.Count) && (diffData.LineCompare[line] == LCS.MatchType.Gap))
				line += direction;
			return line;
		}

		public List<Tuple<int, int>> GetDiffMatches(bool match)
		{
			if (diffData == null)
				throw new ArgumentException("No diff in progress");

			var result = new List<Tuple<int, int>>();
			var matchTuple = default(Tuple<int, int>);
			var line = 0;
			while (true)
			{
				var end = line >= diffData.LineCompare.Count;

				if ((!end) && ((diffData.LineCompare[line] == LCS.MatchType.Match) == match))
					matchTuple = Tuple.Create(matchTuple?.Item1 ?? lineOffset[line], lineOffset[line + 1]);
				else if (matchTuple != null)
				{
					result.Add(matchTuple);
					matchTuple = null;
				}

				if (end)
					break;

				++line;
			}
			return result;
		}
	}
}
