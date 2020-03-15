using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NeoEdit.Program
{
	public class NETextView
	{
		readonly List<int> linePosition;
		readonly List<int> endingPosition;
		public string DefaultEnding { get; private set; }

		public int NumLines => endingPosition.Count;
		public int MaxPosition { get; private set; }
		public int MaxIndex { get; private set; }

		public NETextView(NEText text)
		{
			const int Ending_None = 0;
			const int Ending_CR = 1;
			const int Ending_LF = 2;
			const int Ending_CRLF = 3;
			const int Ending_Mixed = 4;

			linePosition = new List<int>();
			endingPosition = new List<int>();
			MaxIndex = 0;

			var lineEndChars = new char[] { '\r', '\n' };

			var chunkSize = Math.Max(65536, text.Length / 32);
			var startChunk = 0;
			var chunks = new List<Tuple<int, int>>();
			while (startChunk < text.Length)
			{
				var endChunk = text.IndexOfAny(lineEndChars, Math.Min(text.Length, startChunk + chunkSize));
				if (endChunk == -1)
					endChunk = text.Length;
				while ((endChunk < text.Length) && (lineEndChars.Contains(text[endChunk])))
					++endChunk;

				chunks.Add(Tuple.Create(startChunk, endChunk));

				startChunk = endChunk;
			}

			var chunkLinePositions = chunks.Select(chunk => new List<int>()).ToList();
			var chunkEndingPositions = chunks.Select(chunk => new List<int>()).ToList();

			int defaultEnding = Ending_None;
			Parallel.ForEach(chunks, chunk =>
			{
				var index = chunks.IndexOf(chunk);
				int chunkDefaultEnding = Ending_None;
				var chunkLinePosition = chunkLinePositions[index];
				var chunkEndingPosition = chunkEndingPositions[index];
				var chunkMaxIndex = 0;

				var position = chunk.Item1;
				while (position < chunk.Item2)
				{
					var endLine = text.IndexOfAny(lineEndChars, position, chunk.Item2 - position);
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
			MaxPosition = text.Length;
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

		public Range GetLine(int line, bool includeEnding = false)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();
			return Range.FromIndex(linePosition[line], GetLineLength(line) + (includeEnding ? GetEndingLength(line) : 0));
		}

		public Range GetEnding(int line)
		{
			if ((line < 0) || (line >= NumLines))
				throw new IndexOutOfRangeException();
			return Range.FromIndex(endingPosition[line], GetEndingLength(line));
		}

		public int GetPosition(int line, int index, bool allowJustPastEnd = false)
		{
			if ((allowJustPastEnd) && (line == NumLines) && (index == 0))
				return MaxPosition;

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
			if ((position < 0) || (position > MaxPosition))
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
	}
}
