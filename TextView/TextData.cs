using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NeoEdit.Common.Transform;
using NeoEdit.TextView.Dialogs;

namespace NeoEdit.TextView
{
	class TextData
	{
		public string FileName { get; private set; }
		public int NumLines { get; private set; }
		public int NumColumns { get; private set; }
		public long Size { get; private set; }
		FileStream file { get; set; }
		List<long> lineStart { get; set; }
		Coder.CodePage codePage { get; set; }
		public TextData(string filename, Action<TextData> onScanComplete)
		{
			MultiProgressDialog.Run("Scanning file...", new List<string> { filename }, (progress, cancel) =>
			{
				FileName = filename;
				file = File.OpenRead(FileName);
				Size = file.Length;
				var header = Read(0, (int)Math.Min(4, Size));
				codePage = Coder.CodePageFromBOM(header);

				long position = Coder.PreambleSize(codePage);
				var charSize = Coder.CharSize(codePage);
				var bigEndian = (codePage == Coder.CodePage.UTF16BE) || (codePage == Coder.CodePage.UTF32BE);
				int lineLength = 0, maxLine = 0;
				Win32.Interop.GetLinesEncoding getLinesEncoding = Win32.Interop.GetLinesEncoding.Default;
				switch (codePage)
				{
					case Coder.CodePage.UTF8: getLinesEncoding = Win32.Interop.GetLinesEncoding.UTF8; break;
					case Coder.CodePage.UTF16LE: getLinesEncoding = Win32.Interop.GetLinesEncoding.UTF16LE; break;
					case Coder.CodePage.UTF16BE: getLinesEncoding = Win32.Interop.GetLinesEncoding.UTF16BE; break;
					case Coder.CodePage.UTF32LE: getLinesEncoding = Win32.Interop.GetLinesEncoding.UTF32LE; break;
					case Coder.CodePage.UTF32BE: getLinesEncoding = Win32.Interop.GetLinesEncoding.UTF32BE; break;
				}

				var block = new byte[65536];
				var blockSize = block.Length - charSize;

				lineStart = new List<long> { position };
				while (position < Size)
				{
					if (cancel())
						return;

					var use = (int)Math.Min(Size - position, blockSize);
					Read(position, use, block);
					block[use] = 1; // This won't match anything and is written beyond the used array
					if (position + use != Size)
						use -= charSize;

					lineStart.AddRange(Win32.Interop.GetLines(getLinesEncoding, block, use, ref position, ref lineLength, ref maxLine));
					progress(0, (int)(position * 100 / Size));
				}
				if (lineStart.Last() != Size)
				{
					lineStart.Add(Size);
					maxLine = Math.Max(maxLine, lineLength);
				}

				NumLines = lineStart.Count - 1;
				NumColumns = maxLine;
			}, () => onScanComplete(this));
		}

		byte[] Read(long position, int size, byte[] buffer = null)
		{
			if (buffer == null)
				buffer = new byte[size];
			if (buffer.Length < size)
				throw new Exception("Buffer too small");
			file.Position = position;
			if (file.Read(buffer, 0, size) != size)
				throw new Exception("Failed to read whole block");
			return buffer;
		}

		public string GetLine(int line)
		{
			return GetLines(line, line + 1).First();
		}

		string FormatLine(string str)
		{
			str = str.TrimEnd('\r', '\n');
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

		public List<string> GetLines(int startLine, int endLine, bool format = true)
		{
			var encoder = Coder.GetEncoding(codePage);
			var result = new List<string>();
			var startOffset = lineStart[startLine];
			var data = Read(startOffset, (int)(lineStart[endLine] - startOffset));
			for (var line = startLine; line < endLine; ++line)
			{
				var str = encoder.GetString(data, (int)(lineStart[line] - startOffset), (int)(lineStart[line + 1] - lineStart[line]));
				if (format)
					str = FormatLine(str);
				result.Add(str);
			}
			return result;
		}

		public long GetSizeEstimate(int startLine, int endLine)
		{
			return lineStart[endLine] - lineStart[startLine];
		}

		public void Close()
		{
			file.Close();
		}
	}
}
