using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeoEdit.Common;
using NeoEdit.Common.Transform;

namespace NeoEdit.Editor
{
	// Positions: absolute positions in data
	// Lines/indexes: positions in data as broken into lines
	// Columns: Positions accounting for tabs
	// Indexes go from 0 - lineLength+1 (columns have equivalent values):
	// 0 is before first character
	// lineLength is after last character
	// lineLength+1 includes line ending
	public class NEText
	{
		const int tabStop = 4;

		string text;
		List<int> linePosition;
		List<int> endingPosition;
		public string OnlyEnding { get; private set; }
		public string DefaultEnding { get; private set; }

		NETextPoint neTextPoint = new NETextPoint(new List<Range>(), new List<string>(), null);

		public int Length => text.Length;
		public int NumLines => endingPosition.Count;
		public int MaxIndex { get; private set; }
		public int MaxColumn { get; private set; }

		public NEText(string text = "")
		{
			this.text = text;
			CalculateLines();
		}

		public bool CanEncode(Coder.CodePage codePage) => Coder.CanEncode(text, codePage);

		public byte[] GetBytes(Coder.CodePage codePage) => Coder.StringToBytes(text, codePage, true);

		void CalculateLines()
		{
			const int Ending_None = 0;
			const int Ending_CR = 1;
			const int Ending_LF = 2;
			const int Ending_CRLF = 3;
			const int Ending_Mixed = 4;

			linePosition = new List<int>();
			endingPosition = new List<int>();
			MaxIndex = MaxColumn = 0;

			var chunkSize = Math.Max(65536, text.Length / 32);
			var startChunk = 0;
			var chunks = new List<Tuple<int, int>>();
			while (startChunk < text.Length)
			{
				var endChunk = text.IndexOfAny(Helpers.NewLineChars, Math.Min(text.Length, startChunk + chunkSize));
				if (endChunk == -1)
					endChunk = text.Length;
				while ((endChunk < text.Length) && (Helpers.NewLineChars.Contains(text[endChunk])))
					++endChunk;

				chunks.Add(Tuple.Create(startChunk, endChunk));

				startChunk = endChunk;
			}

			var chunkLinePositions = chunks.Select(chunk => new List<int>()).ToList();
			var chunkEndingPositions = chunks.Select(chunk => new List<int>()).ToList();

			int defaultEnding = Ending_None, onlyEnding = Ending_None;
			Parallel.ForEach(chunks, chunk =>
			{
				var index = chunks.IndexOf(chunk);
				int chunkDefaultEnding = Ending_None, chunkOnlyEnding = Ending_None;
				var chunkLinePosition = chunkLinePositions[index];
				var chunkEndingPosition = chunkEndingPositions[index];
				var chunkMaxIndex = 0;

				var position = chunk.Item1;
				while (position < chunk.Item2)
				{
					var endLine = text.IndexOfAny(Helpers.NewLineChars, position, chunk.Item2 - position);
					var endLineLen = 1;
					var ending = Ending_None;
					if (endLine == -1)
					{
						endLine = chunk.Item2;
						endLineLen = 0;
					}
					else if ((endLine + 1 < chunk.Item2) && (text[endLine] == '\r') && (text[endLine + 1] == '\n'))
					{
						++endLineLen;
						ending = Ending_CRLF;
					}
					else
						ending = text[endLine] == '\r' ? Ending_CR : Ending_LF;

					if (ending != Ending_None)
					{
						if (chunkDefaultEnding == Ending_None)
							chunkDefaultEnding = ending;
						if (chunkOnlyEnding == Ending_None)
							chunkOnlyEnding = ending;
						if (chunkOnlyEnding != ending)
							chunkOnlyEnding = Ending_Mixed;
					}
					chunkLinePosition.Add(position);
					chunkEndingPosition.Add(endLine);
					position = endLine + endLineLen;
					chunkMaxIndex = Math.Max(chunkMaxIndex, endLine - position);
				}

				lock (chunkLinePositions)
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

			chunkLinePositions.ForEach(values => linePosition.AddRange(values));
			chunkEndingPositions.ForEach(values => endingPosition.AddRange(values));

			// Always have an ending line
			if ((endingPosition.Count == 0) || (endingPosition.Last() != text.Length))
			{
				linePosition.Add(text.Length);
				endingPosition.Add(text.Length);
			}

			// Used only for calculating length
			linePosition.Add(text.Length);

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

		public char this[int position]
		{
			get
			{
				if ((position < 0) || (position >= text.Length))
					throw new IndexOutOfRangeException();
				return text[position];
			}
		}

		public char this[int line, int index]
		{
			get
			{
				if ((line < 0) || (line >= NumLines))
					throw new IndexOutOfRangeException();
				if ((index < 0) || (index >= GetLineLength(line)))
					throw new IndexOutOfRangeException();
				return text[linePosition[line] + index];
			}
		}

		public int GetLineLength(int line)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();
			return endingPosition[line] - linePosition[line];
		}

		public int GetEndingLength(int line)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();
			return linePosition[line + 1] - endingPosition[line];
		}

		public int GetLineColumnsLength(int line)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();

			var index = linePosition[line];
			var len = GetLineLength(line);
			var columns = 0;
			while (len > 0)
			{
				var find = text.IndexOf('\t', index, len);
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
			return text.Substring(linePosition[line], GetLineLength(line) + (includeEnding ? GetEndingLength(line) : 0));
		}

		public string GetLineColumns(int line, int startColumn, int endColumn)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();

			var column = 0;
			var index = linePosition[line];
			var endIndex = index + GetLineLength(line);
			var sb = new StringBuilder();
			while ((column < endColumn) && (index < endIndex))
			{
				var skipColumns = Math.Max(0, startColumn - column);
				var takeColumns = endColumn - column;

				var tabIndex = text.IndexOf('\t', index, Math.Min(endIndex - index, takeColumns));
				if (tabIndex == index)
				{
					var repeatCount = (column / tabStop + 1) * tabStop - column;
					var useColumns = Math.Min(Math.Max(0, repeatCount - skipColumns), takeColumns);
					sb.Append(' ', useColumns);
					column += repeatCount;
					++index;
					continue;
				}

				if (tabIndex == -1)
					tabIndex = endIndex;

				if (skipColumns > 0)
				{
					var useColumns = Math.Min(skipColumns, tabIndex - index);
					index += useColumns;
					column += useColumns;
				}

				{
					var useColumns = Math.Min(tabIndex - index, takeColumns);
					sb.Append(text, index, useColumns);
					column += useColumns;
					index += useColumns;
				}
			}

			return sb.ToString();
		}

		public List<int> GetLineColumnMap(int line, bool includeEnding = false)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();

			var result = new List<int>();
			var index = linePosition[line];
			var len = GetLineLength(line) + (includeEnding ? GetEndingLength(line) : 0);
			var outPos = 0;
			while (len > 0)
			{
				result.Add(outPos);
				if (text[index] == '\t')
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
			return text.Substring(endingPosition[line], GetEndingLength(line));
		}

		public int GetPosition(int line, int index, bool allowJustPastEnd = false)
		{
			if ((allowJustPastEnd) && (line == NumLines) && (index == 0))
				return text.Length;

			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();
			if ((index < 0) || (index > GetLineLength(line) + 1))
				throw new IndexOutOfRangeException();
			if (index == GetLineLength(line) + 1)
				return endingPosition[line] + GetEndingLength(line);
			return linePosition[line] + index;
		}

		public int GetPositionLine(int position)
		{
			if ((position < 0) || (position > text.Length))
				throw new IndexOutOfRangeException();
			var line = linePosition.BinarySearch(position);
			if (line < 0)
				line = ~line - 1;
			while ((line < linePosition.Count - 2) && (linePosition[line] == linePosition[line + 1]))
				++line;
			if (line == linePosition.Count - 1)
				--line;
			return line;
		}

		public int GetPositionIndex(int position, int line)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();
			if ((position < linePosition[line]) || (position > endingPosition[line] + GetEndingLength(line)))
				throw new IndexOutOfRangeException();
			if (position > endingPosition[line])
				return GetLineLength(line) + 1;
			return position - linePosition[line];
		}

		public int GetColumnFromIndex(int line, int findIndex)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();
			if ((findIndex < 0) || (findIndex > GetLineLength(line) + 1))
				throw new IndexOutOfRangeException();

			var column = 0;
			var position = linePosition[line];
			var findPosition = findIndex + position;
			var end = position + GetLineLength(line);
			while (position < findPosition)
			{
				var find = text.IndexOf('\t', position, end - position);
				if (find == position)
				{
					column = (column / tabStop + 1) * tabStop;
					++position;
					continue;
				}

				if (find == -1)
					find = findPosition - position;
				else
					find = Math.Min(find, findPosition) - position;

				column += find;
				position += find;
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
			var position = linePosition[line];
			var end = position + GetLineLength(line);
			while (column < findColumn)
			{
				var find = text.IndexOf('\t', position, end - position);
				if (find == position)
				{
					column = (column / tabStop + 1) * tabStop;
					++position;
					continue;
				}

				if (find == -1)
					find = findColumn - column;
				else
					find = Math.Min(find - position, findColumn - column);

				column += find;
				position += find;
			}
			if (position > end + 1)
			{
				if (returnMaxOnFail)
					return GetLineLength(line) + 1;
				throw new IndexOutOfRangeException();
			}
			return position - linePosition[line];
		}

		public string GetString() => text;

		public string GetString(Range range) => text.Substring(range.Start, range.Length);

		public string GetString(int start, int length)
		{
			if ((start < 0) || (length < 0) || (start + length > text.Length))
				throw new IndexOutOfRangeException();
			return text.Substring(start, length);
		}

		public NETextPoint CreateTextPoint(IReadOnlyList<Range> ranges, IReadOnlyList<string> strs)
		{
			MoveToTextPoint(new NETextPoint(ranges, strs, neTextPoint));
			return neTextPoint;
		}

		public void MoveToTextPoint(NETextPoint nextNETextPoint)
		{
			text = NETextPoint.MoveTo(text, neTextPoint, nextNETextPoint);
			neTextPoint = nextNETextPoint;
			CalculateLines();
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
				var lineStart = (LineStartTabStop != 0) && (!string.IsNullOrWhiteSpace(line));
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
			public List<DiffType> LineCompare;
			public List<Tuple<int, int>>[] ColCompare;
			public Dictionary<int, int> LineMap, LineRevMap;

			public DiffData(string data, DiffParams diffParams)
			{
				Data = data;
				DiffParams = diffParams;
			}
		}
		DiffData diffData;

		public static void CalculateDiff(NEText textData0, NEText textData1, bool ignoreWhitespace, bool ignoreCase, bool ignoreNumbers, bool ignoreLineEndings, string ignoreCharacters)
		{
			var diffParams = new DiffParams(ignoreWhitespace, ignoreCase, ignoreNumbers, ignoreLineEndings, ignoreCharacters);
			if ((textData0.diffData != null) && (textData1.diffData != null) && (textData0.diffData.Data == textData0.text) && (textData1.diffData.Data == textData1.text) && (textData0.diffData.DiffParams.Equals(diffParams)) && (textData1.diffData.DiffParams.Equals(diffParams)))
				return;

			var textData = new NEText[] { textData0, textData1 };
			var lines = new List<string>[2];
			var map = new List<List<int>>[2];
			for (var pass = 0; pass < 2; ++pass)
			{
				textData[pass].ClearDiff();
				textData[pass].diffData = new DiffData(textData[pass].text, diffParams);
				var formatDiffLine = Enumerable.Range(0, textData[pass].NumLines).Select(line => diffParams.FormatLine(textData[pass].GetLine(line, true))).ToList();
				lines[pass] = formatDiffLine.Select(val => val.Item1).ToList();
				map[pass] = formatDiffLine.Select(val => val.Item2).ToList();
			}

			var linesLCS = LCS.GetLCS(lines[0], lines[1], (str1, str2) => (string.IsNullOrWhiteSpace(str1) == string.IsNullOrWhiteSpace(str2)));

			for (var pass = 0; pass < 2; ++pass)
			{
				textData[pass].diffData.LineCompare = linesLCS.Select(val =>
				{
					if (val[pass] == LCS.MatchType.Match)
						return DiffType.Match;
					if (val[pass] == LCS.MatchType.Gap)
						return DiffType.GapMismatch;
					if (val[1 - pass] == LCS.MatchType.Gap)
						return DiffType.MismatchGap;
					return DiffType.Mismatch;
				}).ToList();
				for (var ctr = 0; ctr < linesLCS.Count; ++ctr)
					if (linesLCS[ctr][pass] == LCS.MatchType.Gap)
					{
						textData[pass].linePosition.Insert(ctr, textData[pass].linePosition[ctr]);
						textData[pass].endingPosition.Insert(ctr, textData[pass].linePosition[ctr]);
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
						skip = true;
					}
				}
				if (skip)
					continue;

				var colsLCS = LCS.GetLCS(lines[0][curLine[0]], lines[1][curLine[1]], (ch1, ch2) => (char.IsLetterOrDigit(ch1) && char.IsLetterOrDigit(ch2)) || (char.IsWhiteSpace(ch1) && char.IsWhiteSpace(ch2)));

				for (var pass = 0; pass < 2; ++pass)
				{
					var start = default(int?);
					var pos = -1;
					textData[pass].diffData.ColCompare[line] = new List<Tuple<int, int>>();
					for (var ctr = 0; ; ++ctr)
					{
						var done = ctr == colsLCS.Count;
						if ((done) || (colsLCS[ctr][pass] != LCS.MatchType.Gap))
							++pos;

						if ((done) || (colsLCS[ctr].IsMatch))
						{
							if (start.HasValue)
								textData[pass].diffData.ColCompare[line].Add(Tuple.Create(map[pass][curLine[pass]][start.Value], map[pass][curLine[pass]][pos]));
							start = null;
						}
						else if (colsLCS[ctr][pass] == LCS.MatchType.Mismatch)
							start = start ?? pos;
						else
							start = start ?? pos + 1;

						if (done)
							break;
					}
				}
			}
		}

		public void ClearDiff()
		{
			if (diffData == null)
				return;

			for (var line = diffData.LineCompare.Count - 1; line >= 0; --line)
				if (diffData.LineCompare[line] == DiffType.GapMismatch)
				{
					linePosition.RemoveAt(line);
					endingPosition.RemoveAt(line);
				}

			diffData = null;
		}

		public List<Tuple<double, double>> GetDiffRanges()
		{
			if (diffData == null)
				return null;

			var diffRanges = new List<Tuple<double, double>>();
			var start = -1;
			var line = -1;
			while (true)
			{
				++line;
				var stop = line >= diffData.LineCompare.Count;
				var lineIsDiff = stop ? false : diffData.LineCompare[line] != DiffType.Match;

				if ((start != -1) && (!lineIsDiff))
				{
					diffRanges.Add(new Tuple<double, double>(start, line));
					start = -1;
				}

				if (stop)
					break;

				if ((start == -1) && (lineIsDiff))
					start = line;
			}

			return diffRanges;
		}

		public static Tuple<List<Tuple<int, int>>, List<string>> GetDiffFixes(NEText src, NEText dest, int lineStartTabStop, bool? ignoreWhitespace, bool? ignoreCase, bool? ignoreNumbers, bool? ignoreLineEndings, string ignoreCharacters)
		{
			var textData = new NEText[] { src, dest };
			var lineMap = new Dictionary<int, int>[2];
			var lines = new List<string>[2];
			var textLines = new List<string>[2];
			var diffParams = new DiffParams(ignoreWhitespace ?? true, ignoreCase ?? true, ignoreNumbers ?? true, ignoreLineEndings ?? true, ignoreCharacters, lineStartTabStop);
			for (var pass = 0; pass < 2; ++pass)
			{
				lineMap[pass] = Enumerable.Range(0, textData[pass].NumLines).Indexes(line => textData[pass].diffData?.LineCompare[line] != DiffType.GapMismatch).Select((index1, index2) => new { index1, index2 }).ToDictionary(obj => obj.index2, obj => obj.index1);
				lines[pass] = lineMap[pass].Values.Select(line => textData[pass].GetLine(line, true)).ToList();
				textLines[pass] = lines[pass].Select(line => diffParams.FormatLine(line).Item1).ToList();
			}

			var linesLCS = LCS.GetLCS(textLines[0], textLines[1], (str1, str2) => string.IsNullOrWhiteSpace(str1) == string.IsNullOrWhiteSpace(str2));

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
										var linePosition = textData[pass].GetPosition(mappedCurLine[pass], 0);
										var begin = linePosition + map[pass][start.Value];
										var end = linePosition + map[pass][pos];
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
					var endingStart = dest.endingPosition[mappedCurLine[1]];
					var endingEnd = dest.linePosition[mappedCurLine[1] + 1];
					if (endingStart == endingEnd)
						continue;

					if (dest.text.Substring(endingStart, endingEnd - endingStart) != src.OnlyEnding)
					{
						ranges.Add(Tuple.Create(endingStart, endingEnd));
						strs.Add(src.OnlyEnding);
					}
				}
			}

			return Tuple.Create(ranges, strs);
		}

		public bool IsDiffGapLine(int line) => diffData == null ? false : diffData.LineCompare[line] == DiffType.GapMismatch;
		public int GetDiffLine(int line) => (diffData == null) || (line >= diffData.LineMap.Count) ? line : diffData.LineMap[line];
		public int GetNonDiffLine(int line) => (diffData == null) || (line >= diffData.LineRevMap.Count) ? line : diffData.LineRevMap[line];
		public bool GetLineDiffMatches(int line) => diffData == null ? true : diffData.LineCompare[line] == DiffType.Match;
		public DiffType GetLineDiffType(int line) => diffData == null ? DiffType.Match : diffData.LineCompare[line];
		public List<Tuple<int, int>> GetLineColumnDiffs(int line) => diffData?.ColCompare[line] ?? new List<Tuple<int, int>>();

		public int SkipDiffGaps(int line, int direction)
		{
			while ((diffData != null) && (line >= 0) && (line < diffData.LineCompare.Count) && (diffData.LineCompare[line] == DiffType.GapMismatch))
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

				if ((!end) && ((diffData.LineCompare[line] == DiffType.Match) == match))
					matchTuple = Tuple.Create(matchTuple?.Item1 ?? linePosition[line], linePosition[line + 1]);
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

		public int IndexOf(char value, int position, int length) => text.IndexOf(value, position, length);
		public int IndexOf(char value, Range range) => text.IndexOf(value, range.Start, range.Length);
		public int IndexOfAny(char[] anyOf, int position) => text.IndexOfAny(anyOf, position);
		public int IndexOfAny(char[] anyOf, int position, int length) => text.IndexOfAny(anyOf, position, length);
		public int IndexOfAny(char[] anyOf, Range range) => text.IndexOfAny(anyOf, range.Start, range.Length);
		public bool Equals(NEText neText) => text == neText.text;

		public int GetLineStartPosition(int position)
		{
			if ((position < 0) || (position > text.Length))
				throw new IndexOutOfRangeException();

			// If we're in the middle of \r\n move back
			if ((position > 0) && (position < text.Length) && (text[position - 1] == '\r') && (text[position] == '\n'))
				--position;

			for (var ctr = position - 1; ctr >= 0; --ctr)
				if ((text[ctr] == '\r') || (text[ctr] == '\n'))
					return ctr + 1;
			return 0;
		}

		public int GetLineEndPosition(int position)
		{
			if ((position < 0) || (position > text.Length))
				throw new IndexOutOfRangeException();

			// If we're in the middle of \r\n move back
			if ((position > 0) && (position < text.Length) && (text[position - 1] == '\r') && (text[position] == '\n'))
				--position;

			for (var ctr = position; ctr < text.Length; ++ctr)
				if ((text[ctr] == '\r') || (text[ctr] == '\n'))
					return ctr;

			return text.Length;
		}

		public int PrevChar(int position, bool allowBeginning = false)
		{
			if ((allowBeginning) && (position == 0))
				return 0;

			if ((position <= 0) || (position > text.Length))
				throw new IndexOutOfRangeException();

			--position;
			if ((position > 0) && (text[position - 1] == '\r') && (text[position] == '\n'))
				--position;
			return position;
		}

		public int NextChar(int position, bool allowEnd = false)
		{
			if ((allowEnd) && (position == text.Length))
				return text.Length;

			if ((position < 0) || (position >= text.Length))
				throw new IndexOutOfRangeException();

			if ((position + 1 < text.Length) && (text[position] == '\r') && (text[position + 1] == '\n'))
				++position;
			++position;
			return position;
		}

		public int GetColumnFromPosition(int position, int lineStartPosition)
		{
			if (position < lineStartPosition)
				throw new IndexOutOfRangeException();

			var column = 0;
			for (var ctr = lineStartPosition; ctr < position; ++ctr)
			{
				if ((text[ctr] == '\r') || (text[ctr] == '\r'))
					throw new IndexOutOfRangeException("Position is not in the given line");
				else if (text[ctr] == '\t')
					column = (column / tabStop + 1) * tabStop;
				else
					++column;
			}
			return column;
		}

		public int GetPositionFromColumn(int findColumn, int lineStartPosition, bool stopAtLineEnd = false)
		{
			if ((lineStartPosition < 0) || (lineStartPosition > text.Length) || (findColumn < 0))
				throw new IndexOutOfRangeException();

			var position = lineStartPosition;
			var column = 0;
			while (column < findColumn)
			{
				if ((position == text.Length) || (text[position] == '\r') || (text[position] == '\n'))
				{
					if (stopAtLineEnd)
						break;
					throw new IndexOutOfRangeException("Line doesn't have column");
				}
				else if (text[position] == '\t')
					column = (column / tabStop + 1) * tabStop;
				else
					++column;
				++position;
			}
			return position;
		}

		public override string ToString() => text;
	}
}
