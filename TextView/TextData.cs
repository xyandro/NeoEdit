using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeoEdit.Common.Transform;
using NeoEdit.TextView.Dialogs;

namespace NeoEdit.TextView
{
	class TextData : IDisposable
	{
		public string FileName { get; private set; }
		public int NumLines { get; private set; }
		public int NumColumns { get; private set; }
		public long Size { get; private set; }
		FileStream file { get; set; }
		List<long> lineStart { get; set; }
		Coder.CodePage codePage { get; set; }
		static public void Create(List<string> fileNames, Action<TextData> onScanComplete)
		{
			var headers = new List<string>();
			if (fileNames.Count != 1)
				headers.Add("Reading files");
			headers.AddRange(fileNames.Select(name => Path.GetFileName(name)));
			MultiProgressDialog.Run("Scanning file...", headers, (progress, cancel) =>
			{
				var files = fileNames.Select(file => new TextData(file)).ToList();
				var totalRead = 0L;
				var totalSize = files.Sum(a => a.Size);
				var lockObj = new object();

				Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = 4 }, (file, state, index) =>
				{
					try
					{
						var lastRead = 0L;
						if (file.Scan((read, total) =>
						{
							if (fileNames.Count != 1)
								progress((int)index + 1, read, total);
							lock (lockObj)
								totalRead += read - lastRead;
							lastRead = read;
							progress(0, totalRead, totalSize);
						}, () => cancel()))
							onScanComplete(file);
					}
					catch
					{
						cancel(true);
						throw;
					}
				});
			});
		}

		TextData(string filename)
		{
			FileName = filename;
			file = File.OpenRead(FileName);
			Size = file.Length;
			var header = Read(0, (int)Math.Min(4, Size));
			codePage = Coder.CodePageFromBOM(header);
		}

		bool Scan(Action<long, long> progress, Func<bool> cancel)
		{
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
				{
					Dispose();
					return false;
				}

				var use = (int)Math.Min(Size - position, blockSize);
				Read(position, use, block);
				block[use] = 1; // This won't match anything and is written beyond the used array
				if (position + use != Size)
					use -= charSize;

				lineStart.AddRange(Win32.Interop.GetLines(getLinesEncoding, block, use, ref position, ref lineLength, ref maxLine));
				progress(position, Size);
			}
			if (lineStart.Last() != Size)
			{
				lineStart.Add(Size);
				maxLine = Math.Max(maxLine, lineLength);
			}

			NumLines = lineStart.Count - 1;
			NumColumns = maxLine;
			return true;
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

		public void Dispose()
		{
			if (file != null)
			{
				file.Dispose();
				file = null;
			}
		}
		public List<Tuple<string, long, long>> CalculateSplit(string format, long minSize)
		{
			var headerSize = Coder.PreambleSize(codePage);
			if (minSize <= 0)
				return new List<Tuple<string, long, long>>();

			var result = new List<Tuple<string, long, long>>();
			var fileNum = 0;
			var end = 0;
			while (true)
			{
				var start = end;
				end = lineStart.BinarySearch(lineStart[start] + minSize - headerSize);
				if (end < 0)
					end = ~end;
				end = Math.Min(Math.Max(start + 1, end), lineStart.Count - 1);
				if (start == end)
					break;
				result.Add(Tuple.Create(String.Format(format, ++fileNum), lineStart[start], lineStart[end]));
			}
			return result;
		}

		public int CalculateSplit(long minSize)
		{
			var headerSize = Coder.PreambleSize(codePage);
			if (minSize <= 0)
				return 0;

			var count = 0;
			var end = 0;
			while (true)
			{
				var start = end;
				end = lineStart.BinarySearch(lineStart[start] + minSize - headerSize);
				if (end < 0)
					end = ~end;
				end = Math.Min(Math.Max(start + 1, end), lineStart.Count - 1);
				if (start == end)
					break;
				++count;
			}
			return count;
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
					long total = files.Select(file => fileLengths[file]).Sum() - header.Length * files.Count + header.Length;
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
