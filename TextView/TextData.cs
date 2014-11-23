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
					progress(0, position, Size);
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

		static byte[] Read(FileStream file, long position, int size, byte[] buffer = null)
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

		byte[] Read(long position, int size, byte[] buffer = null)
		{
			return Read(file, position, size, buffer);
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

		public List<Tuple<string, long, long>> CalculateSplit(string format, long minSize)
		{
			if (minSize <= 0)
				return new List<Tuple<string, long, long>>();

			var result = new List<Tuple<string, long, long>>();
			var header = Coder.PreambleSize(codePage);
			var fileNum = 0;
			var line = 0;
			while (true)
			{
				var start = lineStart[line];
				line = lineStart.BinarySearch(start + minSize);
				if (line < 0)
					line = ~line;
				line = Math.Min(line, lineStart.Count - 1);
				var end = lineStart[line];
				if (start == end)
					break;
				result.Add(Tuple.Create(String.Format(format, ++fileNum), start, end));
			}
			if (result.Count <= 1)
				return new List<Tuple<string, long, long>>();
			return result;
		}

		public void SplitFile(List<Tuple<string, long, long>> splitData)
		{
			var names = new List<string> { FileName };
			names.AddRange(splitData.Select(tuple => tuple.Item1));
			names = names.Select(file => Path.GetFileName(file)).ToList();

			MultiProgressDialog.Run("Splitting files...", names, (progress, cancel) =>
			{
				var header = Read(0, Coder.PreambleSize(codePage));
				long inputRead = 0;
				long inputSize = Size - header.Length;
				var buffer = new byte[65536];
				for (var ctr = 0; ctr < splitData.Count; ++ctr)
				{
					var item = splitData[ctr];
					using (var file = File.Create(item.Item1))
					{
						file.Write(header, 0, header.Length);
						long outputWritten = 0;
						long outputSize = item.Item3 - item.Item2;
						while (outputWritten < outputSize)
						{
							if (cancel())
								return;

							var block = (int)Math.Min(buffer.Length, outputSize - outputWritten);
							Read(outputWritten + item.Item2, block, buffer);
							inputRead += block;
							progress(0, inputRead, inputSize);
							file.Write(buffer, 0, block);
							outputWritten += block;
							progress(ctr + 1, outputWritten, outputSize);
						}
					}
				}
			});
		}

		public static void CombineFiles(string outputFile, List<string> files, Action finished)
		{
			var names = new List<string> { outputFile };
			names.AddRange(files);
			names = names.Select(file => Path.GetFileName(file)).ToList();

			MultiProgressDialog.Run("Combining files...", names, (progress, cancel) =>
			{
				var fileStreams = new Dictionary<string, FileStream>();
				try
				{
					Coder.CodePage? codePage = null;
					byte[] header = null;
					var fileLengths = new Dictionary<string, long>();
					foreach (var file in files)
					{
						if (fileStreams.ContainsKey(file))
							continue;

						fileStreams[file] = File.OpenRead(file);
						fileLengths[file] = fileStreams[file].Length;

						var fileHeader = Read(fileStreams[file], 0, (int)Math.Min(fileStreams[file].Length, 4));
						var fileCodePage = Coder.CodePageFromBOM(fileHeader);
						if (codePage == null)
							codePage = fileCodePage;
						if (codePage != fileCodePage)
							throw new Exception("All files must have the same encoding to combine them.");
						if (header == null)
						{
							header = new byte[Coder.PreambleSize(fileCodePage)];
							Array.Copy(fileHeader, header, header.Length);
						}
					}

					var buffer = new byte[65536];
					long written = 0;
					long total = fileLengths.Values.Sum() - header.Length * fileLengths.Count + header.Length;
					using (var output = File.Create(outputFile))
					{
						output.Write(header, 0, header.Length);
						written += header.Length;
						for (var ctr = 0; ctr < files.Count; ++ctr)
						{
							var file = files[ctr];
							long filePosition = header.Length;
							while (filePosition < fileLengths[file])
							{
								if (cancel())
									return;

								var block = (int)Math.Min(buffer.Length, fileLengths[file] - filePosition);
								Read(fileStreams[file], filePosition, block, buffer);
								filePosition += block;
								progress(ctr + 1, filePosition, fileLengths[file]);

								output.Write(buffer, 0, block);
								written += block;
								progress(0, written, total);
							}
						}
					}
				}
				finally
				{
					var close = fileStreams.Values.ToList();
					foreach (var file in close)
						try { file.Dispose(); }
						catch { }
				}
			}, finished);
		}
	}
}
