using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NeoEdit.Common
{
	public class TextData
	{
		static Regex RecordsRE = new Regex("(.*?)(\r\n|\n\r|\n|\r|$)");
		Stack<string> undoHistory = new Stack<string>();
		Stack<string> redoHistory = new Stack<string>();
		string data { get { return undoHistory.First(); } set { undoHistory.Push(value); redoHistory.Clear(); RecalculateLines(); } }
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
		}

		public void Undo()
		{
			if (undoHistory.Count == 1)
				return;

			redoHistory.Push(undoHistory.Pop());
			RecalculateLines();
		}

		public void Redo()
		{
			if (redoHistory.Count == 0)
				return;

			undoHistory.Push(redoHistory.Pop());
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

		public int GetOffset(int line, int index)
		{
			if ((line < 0) || (line >= endingIndex.Count))
				throw new IndexOutOfRangeException();
			return lineIndex[line] + index;
		}

		public int GetOffsetLine(int offset)
		{
			int line;
			for (line = 0; (line < lineIndex.Count) && (offset >= lineIndex[line]); line++) ;
			return --line;
		}

		public int GetOffsetIndex(int offset, int line)
		{
			return offset - lineIndex[line];
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

		public int NumLines { get { return lineIndex.Count; } }
	}
}
