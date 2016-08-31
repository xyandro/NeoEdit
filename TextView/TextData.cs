﻿using System;
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
		public string FileName { get; }
		public int NumLines { get; private set; }
		public int NumColumns { get; private set; }
		public long Size { get; }
		FileStream file { get; set; }
		List<long> lineStart { get; set; }
		Coder.CodePage codePage { get; }
		static public void ReadFiles(List<string> fileNames, Action<TextData, bool> onFileScanComplete = null, Action<List<TextData>> onAllScanComplete = null)
		{
			if (onFileScanComplete == null) onFileScanComplete = (result, cancelled) => { };
			if (onAllScanComplete == null) onAllScanComplete = result => { };

			var headers = new List<string>();
			if (fileNames.Count != 1)
				headers.Add("Reading files");
			headers.AddRange(fileNames.Select(name => Path.GetFileName(name)));

			var files = fileNames.Select(file => new TextData(file)).ToList();

			MultiProgressDialog.Run("Scanning file...", headers, (progress, cancel) =>
			{
				var totalRead = 0L;
				var totalSize = files.Sum(a => a.Size);
				var lockObj = new object();
				int num = 0;

				Parallel.ForEach(files, ignored =>
				{
					try
					{
						int index;
						lock (lockObj)
							index = num++;
						var file = files[index];
						var lastRead = 0L;
						var cancelled = !file.Scan((read, total) =>
						{
							if (fileNames.Count != 1)
								progress(index + 1, read, total);
							lock (lockObj)
								totalRead += read - lastRead;
							lastRead = read;
							progress(0, totalRead, totalSize);
						}, () => cancel());
						onFileScanComplete(file, cancelled);
					}
					catch
					{
						cancel(true);
						throw;
					}
				});
			}, () => onAllScanComplete(files));
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
			var result = true;

			long position = Coder.PreambleSize(codePage);
			int lineLength = 0, maxLine = 0;

			var block = new byte[65536];

			lineStart = new List<long> { position };
			while (position < Size)
			{
				if (cancel())
				{
					result = false;
					break;
				}

				if (Size - position < block.Length)
					block = new byte[Size - position];
				Read(position, block.Length, block);

				switch (codePage)
				{
					case Coder.CodePage.Default: lineStart.AddRange(Win32.Interop.GetLinesDefault(block, ref lineLength, ref maxLine).Select(offset => offset + position)); break;
					case Coder.CodePage.UTF8: lineStart.AddRange(Win32.Interop.GetLinesUTF8(block, ref lineLength, ref maxLine).Select(offset => offset + position)); break;
					case Coder.CodePage.UTF16LE: lineStart.AddRange(Win32.Interop.GetLinesUTF16LE(block, ref lineLength, ref maxLine).Select(offset => offset + position)); break;
					case Coder.CodePage.UTF16BE: lineStart.AddRange(Win32.Interop.GetLinesUTF16BE(block, ref lineLength, ref maxLine).Select(offset => offset + position)); break;
					case Coder.CodePage.UTF32LE: lineStart.AddRange(Win32.Interop.GetLinesUTF32LE(block, ref lineLength, ref maxLine).Select(offset => offset + position)); break;
					case Coder.CodePage.UTF32BE: lineStart.AddRange(Win32.Interop.GetLinesUTF32BE(block, ref lineLength, ref maxLine).Select(offset => offset + position)); break;
				}

				position += block.Length;
				if ((position != Size) && (lineStart.Last() == position))
				{
					// We have a newline at the end of the block but it may have been split over blocks
					lineStart.RemoveAt(lineStart.Count - 1);
					position = Math.Max(lineStart.Last(), position - 2);
				}
				progress(position, Size);
			}
			if ((position >= Size) && (lineStart.Last() != Size))
			{
				lineStart.Add(Size);
				maxLine = Math.Max(maxLine, lineLength);
			}

			NumLines = lineStart.Count - 1;
			NumColumns = maxLine;
			return result;
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

		static object readLock = new object();
		byte[] Read(long position, int size, byte[] buffer = null)
		{
			lock (readLock)
				return Read(file, position, size, buffer);
		}

		public string GetLine(int line) => GetLines(line, line + 1).First();

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

		public long GetSizeEstimate(int startLine, int endLine) => lineStart[endLine] - lineStart[startLine];

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
				result.Add(Tuple.Create(string.Format(format, ++fileNum), lineStart[start], lineStart[end]));
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

		public static void MergeFiles(string outputFile, List<string> fileNames, Action finished)
		{
			TextData.ReadFiles(fileNames.Distinct().ToList(), null, files =>
			{
				var headers = new List<string> { outputFile };
				headers.AddRange(fileNames);
				headers = headers.Select(file => Path.GetFileName(file)).ToList();

				MultiProgressDialog.Run("Merging files...", headers, (progress, cancel) =>
				{
					try
					{
						using (var output = File.CreateText(outputFile))
						{
							var fileProgressNum = files.GroupJoin(fileNames.Select((file, index) => new { file = file, resultIndex = index + 1 }), file => file.FileName, file => file.file, (item, group) => new { key = item, value = group.Select(file => file.resultIndex).ToList() }).ToDictionary(obj => obj.key, obj => obj.value);
							var fileLine = files.ToDictionary(file => file, file => 0);
							var cacheStartLine = new Dictionary<TextData, int>();
							var cacheLines = new Dictionary<TextData, List<string>>();

							var linesWritten = 0;
							var linesTotal = fileProgressNum.Sum(file => file.Key.NumLines * file.Value.Count);

							while (true)
							{
								foreach (var file in fileLine.Keys.ToList())
								{
									if ((!cacheStartLine.ContainsKey(file)) || (cacheStartLine[file] + cacheLines[file].Count <= fileLine[file]))
									{
										if (fileLine[file] >= file.NumLines)
										{
											fileLine.Remove(file);
											continue;
										}
										cacheStartLine[file] = fileLine[file];
										cacheLines[file] = file.GetLines(cacheStartLine[file], Math.Min(cacheStartLine[file] + 2, file.NumLines), false);
									}
								}

								if (!fileLine.Keys.Any())
									break;

								string minLine = null;
								TextData minLineFile = null;
								foreach (var file in fileLine.Keys)
								{
									var fileCurLine = cacheLines[file][fileLine[file] - cacheStartLine[file]];
									if ((minLine == null) || (fileCurLine.CompareTo(minLine) < 0))
									{
										minLine = fileCurLine;
										minLineFile = file;
									}
								}

								++fileLine[minLineFile];
								foreach (var progressNum in fileProgressNum[minLineFile])
								{
									output.Write(minLine);
									++linesWritten;
									progress(progressNum, fileLine[minLineFile], minLineFile.NumLines);
								}

								progress(0, linesWritten, linesTotal);
								if (cancel())
									return;
							}
						}
					}
					finally
					{
						files.ForEach(file => { try { file.Dispose(); } catch { } });
					}
				}, finished);
			});
		}

		public static void SaveEncoding(string inputFile, string outputFile, Coder.CodePage outCodePage)
		{
			MultiProgressDialog.Run("Changing encoding...", new List<string> { inputFile }, (progress, cancel) =>
			{
				using (var input = File.OpenRead(inputFile))
				{
					var header = Read(input, 0, (int)Math.Min(input.Length, 4));
					var inCodePage = Coder.CodePageFromBOM(header);

					if (inCodePage == outCodePage)
						throw new Exception("File already has that encoding.");

					long position = Coder.PreambleSize(inCodePage);
					var size = input.Length;
					using (var output = File.Create(outputFile))
					{
						var bom = Coder.StringToBytes("", outCodePage, true);
						output.Write(bom, 0, bom.Length);
						var decoder = Coder.GetEncoding(inCodePage).GetDecoder();
						var encoder = Coder.GetEncoding(outCodePage).GetEncoder();
						var chars = new char[65536];
						var bytes = new byte[chars.Length * 5]; // Should be big enough to hold any resulting output
						while (position < size)
						{
							if (cancel())
								return;

							var inByteCount = (int)Math.Min(chars.Length, size - position);
							TextData.Read(input, position, inByteCount, bytes);

							var numChars = decoder.GetChars(bytes, 0, inByteCount, chars, 0);
							var outByteCount = encoder.GetBytes(chars, 0, numChars, bytes, 0, false);

							output.Write(bytes, 0, outByteCount);

							position += inByteCount;
							progress(0, position, size);
						}
					}
				}
			});
		}

		public override string ToString() => FileName;
	}
}
