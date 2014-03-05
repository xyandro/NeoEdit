using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NeoEdit.GUI.Data;

namespace NeoEdit.GUI.Common
{
	public class TextData
	{
		string _data;
		string data { get { return _data; } set { _data = value; RecalculateLines(); } }
		List<int> lineIndex;
		List<int> lineLength;
		List<int> endingIndex;
		List<int> endingLength;
		public string DefaultEnding { get; private set; }
		public Coder.Type CoderUsed { get; private set; }

		public int NumLines { get { return lineIndex.Count; } }
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

		static Regex RecordsRE = new Regex("(.*?)(\r\n|\n\r|\n|\r|$)");
		void RecalculateLines()
		{
			lineIndex = new List<int>();
			lineLength = new List<int>();
			endingIndex = new List<int>();
			endingLength = new List<int>();

			BOM = (data.Length > 0) && (data[0] == '\ufeff');
			var matches = RecordsRE.Matches(data, BOM ? 1 : 0);

			foreach (Match match in matches)
			{
				if ((match.Groups[1].Length == 0) && (match.Groups[2].Length == 0))
					continue;
				lineIndex.Add(match.Groups[1].Index);
				lineLength.Add(match.Groups[1].Length);
				endingIndex.Add(match.Groups[2].Index);
				endingLength.Add(match.Groups[2].Length);
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
