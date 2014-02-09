using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NeoEdit.Common
{
	public class TextData
	{
		static Regex RecordsRE = new Regex("(.*?)(\r\n|\n\r|\n|\r|$)");
		string data;
		List<int> lineIndex;
		List<int> lineLength;
		List<int> endingIndex;
		List<int> endingLength;
		string defaultEnding = "\r\n";

		public TextData(BinaryData binaryData, BinaryData.EncodingName encoding = BinaryData.EncodingName.None)
		{
			if (encoding == BinaryData.EncodingName.None)
				encoding = binaryData.GuessEncoding();

			data = binaryData.ToString(encoding);
			RecalculateLines();
		}

		void RecalculateLines()
		{
			lineIndex = new List<int>();
			lineLength = new List<int>();
			endingIndex = new List<int>();
			endingLength = new List<int>();

			var bom = (data.Length > 0) && (data[0] == '\ufeff');
			var matches = RecordsRE.Matches(data, bom ? 1 : 0);

			foreach (Match match in matches)
			{
				if ((match.Groups[1].Length == 0) && (match.Groups[2].Length == 0))
					continue;
				lineIndex.Add(match.Groups[1].Index);
				lineLength.Add(match.Groups[1].Length);
				endingIndex.Add(match.Groups[2].Index);
				endingLength.Add(match.Groups[2].Length);
			}

			// Select most popular line ending
			defaultEnding = Enumerable.Range(0, endingIndex.Count).Select(a => GetEnding(a)).GroupBy(a => a).OrderByDescending(a => a.Count()).Select(a => a.Key).FirstOrDefault() ?? "\r\n";
		}

		public string this[int line] { get { return GetLine(line); } }

		public string GetLine(int line)
		{
			if ((line < 0) || (line >= lineIndex.Count))
				throw new IndexOutOfRangeException();
			return data.Substring(lineIndex[line], lineLength[line]);
		}

		public string GetEnding(int line)
		{
			if ((line < 0) || (line >= endingIndex.Count))
				throw new IndexOutOfRangeException();
			return data.Substring(endingIndex[line], endingLength[line]);
		}

		public BinaryData GetData(BinaryData.EncodingName encoding)
		{
			return BinaryData.FromString(encoding, data);
		}

		public string GetString(int startLine, int startIndex, int endLine, int endIndex)
		{
			if ((startLine < 0) || (startLine >= endingIndex.Count))
				throw new IndexOutOfRangeException();
			if ((endLine < 0) || (endLine >= endingIndex.Count))
				throw new IndexOutOfRangeException();
			var start = lineIndex[startLine] + startIndex;
			var end = lineIndex[endLine] + endIndex;
			return data.Substring(start, end - start);
		}

		public int NumLines { get { return lineIndex.Count; } }
	}
}
