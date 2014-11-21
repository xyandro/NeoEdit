using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NeoEdit.Common.Transform;

namespace NeoEdit.TextView
{
	class TextData
	{
		public int NumLines { get { return lines; } }

		readonly FileStream file;
		readonly long length;
		readonly int lines;
		readonly List<long> lineStart;
		readonly Encoding encoder;
		public TextData(string filename)
		{
			file = File.OpenRead(filename);
			length = file.Length;
			var header = Read(0, (int)Math.Min(4, length));
			var codePage = StrCoder.CodePageFromBOM(header);

			long position = 0;
			int charSize = 1;
			switch (codePage)
			{
				case StrCoder.CodePage.UTF8: position = 3; break;
				case StrCoder.CodePage.UTF16LE: position = charSize = 2; break;
				case StrCoder.CodePage.UTF16BE: position = charSize = 2; break;
				case StrCoder.CodePage.UTF32LE: position = charSize = 4; break;
				case StrCoder.CodePage.UTF32BE: position = charSize = 4; break;
			}

			encoder = StrCoder.GetEncoding(codePage);

			lineStart = new List<long> { position };
			while (position != length)
			{
				var block = Read(position, (int)Math.Min(length - position, 65536));
				var use = block.Length - ((position + block.Length == length) ? 0 : charSize);
				int ctr, endLen = 0;
				for (ctr = 0; ctr < use; ctr += charSize)
				{
					switch (codePage)
					{
						case StrCoder.CodePage.Default:
						case StrCoder.CodePage.UTF8:
							if ((block[ctr + 0] == '\r') || (block[ctr + 0] == '\n')) endLen = 1;
							if (((block[ctr + 0] == '\r') && (block[ctr + 1] == '\n')) || ((block[ctr + 0] == '\n') && (block[ctr + 1] == '\r'))) endLen = 2;
							break;
						case StrCoder.CodePage.UTF16LE:
							if (((block[ctr + 0] == '\r') && (block[ctr + 1] == 0)) || ((block[ctr + 0] == '\n') && (block[ctr + 1] == 0))) endLen = 2;
							if (((block[ctr + 0] == '\r') && (block[ctr + 1] == 0) && (block[ctr + 2] == '\n') && (block[ctr + 3] == 0)) || ((block[ctr + 0] == '\n') && (block[ctr + 1] == 0) && (block[ctr + 2] == '\r') && (block[ctr + 3] == 0))) endLen = 4;
							break;
						case StrCoder.CodePage.UTF16BE:
							if (((block[ctr + 0] == 0) && (block[ctr + 1] == '\r')) || ((block[ctr + 0] == 0) && (block[ctr + 1] == '\n'))) endLen = 2;
							if (((block[ctr + 0] == 0) && (block[ctr + 1] == '\r') && (block[ctr + 2] == 0) && (block[ctr + 3] == '\n')) || ((block[ctr + 0] == 0) && (block[ctr + 1] == '\n') && (block[ctr + 2] == 0) && (block[ctr + 3] == '\r'))) endLen = 4;
							break;
						case StrCoder.CodePage.UTF32LE:
							if (((block[ctr + 0] == '\r') && (block[ctr + 1] == 0) && (block[ctr + 2] == 0) && (block[ctr + 3] == 0)) || ((block[ctr + 0] == '\n') && (block[ctr + 1] == 0) && (block[ctr + 2] == 0) && (block[ctr + 3] == 0))) endLen = 4;
							if (((block[ctr + 0] == '\r') && (block[ctr + 1] == 0) && (block[ctr + 2] == 0) && (block[ctr + 3] == 0) && (block[ctr + 4] == '\n') && (block[ctr + 5] == 0) && (block[ctr + 6] == 0) && (block[ctr + 7] == 0)) || ((block[ctr + 0] == '\n') && (block[ctr + 1] == 0) && (block[ctr + 2] == 0) && (block[ctr + 3] == 0) && (block[ctr + 4] == '\r') && (block[ctr + 5] == 0) && (block[ctr + 6] == 0) && (block[ctr + 7] == 0))) endLen = 8;
							break;
						case StrCoder.CodePage.UTF32BE:
							if (((block[ctr + 0] == 0) && (block[ctr + 1] == 0) && (block[ctr + 2] == 0) && (block[ctr + 3] == '\r')) || ((block[ctr + 0] == 0) && (block[ctr + 1] == 0) && (block[ctr + 2] == 0) && (block[ctr + 3] == '\n'))) endLen = 4;
							if (((block[ctr + 0] == 0) && (block[ctr + 1] == 0) && (block[ctr + 2] == 0) && (block[ctr + 3] == '\r') && (block[ctr + 4] == 0) && (block[ctr + 5] == 0) && (block[ctr + 6] == 0) && (block[ctr + 7] == '\n')) || ((block[ctr + 0] == 0) && (block[ctr + 1] == 0) && (block[ctr + 2] == 0) && (block[ctr + 3] == '\n') && (block[ctr + 4] == 0) && (block[ctr + 5] == 0) && (block[ctr + 6] == 0) && (block[ctr + 7] == '\r'))) endLen = 8;
							break;
					}

					if (endLen != 0)
					{
						lineStart.Add(position + ctr + endLen);
						ctr += endLen - charSize;
						endLen = 0;
					}
				}
				position += ctr;
			}
			if (lineStart.Last() != length)
				lineStart.Add(length);
			lines = lineStart.Count - 1;
		}

		byte[] Read(long position, int size)
		{
			var result = new byte[size];
			file.Position = position;
			if (file.Read(result, 0, result.Length) != size)
				throw new Exception("Failed to read whole block");
			return result;
		}

		public string GetLine(int line)
		{
			return GetLines(line, line + 1).First();
		}

		string TabFormatLine(string str)
		{
			const int tabStop = 4;
			var index = 0;
			var sb = new StringBuilder();
			while (index < str.Length)
			{
				var find = str.IndexOf('\t', index);
				if (find == index)
				{
					sb.Append(' ', (sb.Length / tabStop + 1) * tabStop - sb.Length);
					++index;
					continue;
				}

				if (find == -1)
					find = str.Length - index;
				else
					find -= index;
				sb.Append(str, index, find);
				index += find;
			}

			return sb.ToString();
		}

		public List<string> GetLines(int startLine, int endLine)
		{
			var result = new List<string>();
			var startOffset = lineStart[startLine];
			var data = Read(startOffset, (int)(lineStart[endLine] - startOffset));
			for (var line = startLine; line < endLine; ++line)
				result.Add(TabFormatLine(encoder.GetString(data, (int)(lineStart[line] - startOffset), (int)(lineStart[line + 1] - lineStart[line])).TrimEnd('\r', '\n')));
			return result;
		}
	}
}
