using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NeoEdit.Program.Dialogs;
using NeoEdit.Program.Expressions;
using NeoEdit.Program.Parsing;
using NeoEdit.Program.Searchers;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program
{
	partial class TextEditor
	{
		async Task<bool> BinarySearchFileAsync(string fileName, BinarySearcher searcher, IProgress<ProgressReport> progress, CancellationToken cancel)
		{
			try
			{
				if (savedAnswers[nameof(BinarySearchFileAsync)].HasFlag(MessageOptions.Cancel))
					return true;

				if (Directory.Exists(fileName))
					return false;

				return await searcher.FindAsync(fileName, progress, cancel);
			}
			catch (Exception ex)
			{
				if (!savedAnswers[nameof(BinarySearchFileAsync)].HasFlag(MessageOptions.All))
					TabsParent.Dispatcher.Invoke(() =>
					{
						savedAnswers[nameof(BinarySearchFileAsync)] = new Message(TabsParent)
						{
							Title = "Confirm",
							Text = $"Unable to read {fileName}.\n\n{ex.Message}\n\nLeave selected?",
							Options = MessageOptions.YesNoAllCancel,
							DefaultCancel = MessageOptions.Cancel,
						}.Show(false);
					});

				return (savedAnswers[nameof(BinarySearchFileAsync)].HasFlag(MessageOptions.Yes)) || (savedAnswers[nameof(BinarySearchFileAsync)].HasFlag(MessageOptions.Cancel));
			}
		}

		async Task<bool> TextSearchFileAsync(string fileName, ISearcher searcher, IProgress<ProgressReport> progress, CancellationToken cancel)
		{
			try
			{
				if (savedAnswers[nameof(TextSearchFileAsync)].HasFlag(MessageOptions.Cancel))
					return true;

				if (Directory.Exists(fileName))
					return false;

				byte[] buffer;
				using (var input = File.OpenRead(fileName))
				{
					buffer = new byte[input.Length];
					var read = 0;
					while ((read < input.Length) && (!cancel.IsCancellationRequested))
					{
						var block = await input.ReadAsync(buffer, read, (int)(input.Length - read));
						progress.Report(new ProgressReport(input.Position, input.Length));
						read += block;
					}
				}

				var data = new TextData(Coder.BytesToString(buffer, Coder.CodePage.AutoByBOM, true));
				var start = data.GetPosition(0, 0);
				return searcher.Find(data.GetString(start, data.MaxPosition - start), start).Any();
			}
			catch (Exception ex)
			{
				TabsParent.Dispatcher.Invoke(() =>
				{
					if (!savedAnswers[nameof(TextSearchFileAsync)].HasFlag(MessageOptions.All))
						savedAnswers[nameof(TextSearchFileAsync)] = new Message(TabsParent)
						{
							Title = "Confirm",
							Text = $"Unable to read {fileName}.\n\n{ex.Message}\n\nLeave selected?",
							Options = MessageOptions.YesNoAllCancel,
							DefaultCancel = MessageOptions.Cancel,
						}.Show(false);
				});

				return (savedAnswers[nameof(TextSearchFileAsync)].HasFlag(MessageOptions.Yes)) || (savedAnswers[nameof(TextSearchFileAsync)].HasFlag(MessageOptions.Cancel));
			}
		}

		static async Task CombineFilesAsync(string outputFile, List<string> inputFiles, IProgress<ProgressReport> progress, CancellationToken cancel)
		{
			var total = inputFiles.Sum(file => new FileInfo(file).Length);
			var written = 0L;
			var buffer = new byte[65536];
			using (var outputStream = File.Create(outputFile))
				foreach (var inputFile in inputFiles)
					using (var inputStream = File.OpenRead(inputFile))
						while (true)
						{
							if (cancel.IsCancellationRequested)
								return;

							var block = await inputStream.ReadAsync(buffer, 0, buffer.Length);
							if (block == 0)
								break;
							await outputStream.WriteAsync(buffer, 0, block);
							written += block;
							progress.Report(new ProgressReport(written, total));
						}
		}

		static void CopyDirectory(string src, string dest)
		{
			var srcDirs = new List<string> { src };
			for (var ctr = 0; ctr < srcDirs.Count; ++ctr)
				srcDirs.AddRange(Directory.GetDirectories(srcDirs[ctr]));

			var srcFiles = new List<string>();
			foreach (var dir in srcDirs)
				srcFiles.AddRange(Directory.GetFiles(dir));

			var destDirs = srcDirs.Select(dir => dest + dir.Substring(src.Length)).ToList();
			var destFiles = srcFiles.Select(file => dest + file.Substring(src.Length)).ToList();
			destDirs.ForEach(dir => Directory.CreateDirectory(dir));
			for (var ctr = 0; ctr < srcFiles.Count; ++ctr)
				File.Copy(srcFiles[ctr], destFiles[ctr]);
		}

		static void FindCommonLength(string str1, string str2, ref int length)
		{
			length = Math.Min(length, Math.Min(str1.Length, str2.Length));
			for (var ctr = 0; ctr < length; ++ctr)
				if (char.ToLowerInvariant(str1[ctr]) != (char.ToLowerInvariant(str2[ctr])))
				{
					length = ctr;
					break;
				}
		}

		static int GetDepthLength(string path, int matchDepth)
		{
			if (matchDepth == 0)
				return 0;

			var depth = 0;
			for (var index = 0; index < path.Length; ++index)
				if (path[index] == '\\')
					if (++depth == matchDepth)
						return index;

			return path.Length;
		}

		static List<string> GetDirectoryContents(string dir, bool recursive, List<string> errors)
		{
			var dirs = new Queue<string>();
			dirs.Enqueue(dir);
			var results = new List<string>();
			int errorCount = 0;
			while (dirs.Count != 0)
			{
				try
				{
					var cur = dirs.Dequeue();
					foreach (var subDir in Directory.GetDirectories(cur))
					{
						dirs.Enqueue(subDir);
						results.Add(subDir);
					}
					results.AddRange(Directory.GetFiles(cur));
				}
				catch (Exception ex)
				{
					++errorCount;
					if (errors.Count < 10)
						errors.Add(ex.Message);
				}
				if (!recursive)
					break;
			}
			if (errorCount != errors.Count)
				errors.Add($"(Plus {errorCount - errors.Count} more)");
			return results;
		}

		Range GetPathRange(GetPathType type, Range range)
		{
			var path = GetString(range);
			var dirLength = Math.Max(0, path.LastIndexOf('\\'));
			if ((path.StartsWith(@"\\")) && (dirLength == 1))
				dirLength = 0;
			var dirTotal = dirLength == 0 ? 0 : dirLength + 1;
			var extLen = Path.GetExtension(path).Length;

			switch (type)
			{
				case GetPathType.FileName: return new Range(range.End, range.Start + dirTotal);
				case GetPathType.FileNameWoExtension: return new Range(range.End - extLen, range.Start + dirTotal);
				case GetPathType.Directory: return Range.FromIndex(range.Start, dirLength);
				case GetPathType.Extension: return new Range(range.End, range.End - extLen);
				default: throw new ArgumentException();
			}
		}

		static string GetRelativePath(string absolutePath, string relativeDirectory)
		{
			var absoluteDirs = absolutePath.Split('\\').ToList();
			var relativeDirs = relativeDirectory.Split('\\').ToList();
			var use = 0;
			while ((use < absoluteDirs.Count) && (use < relativeDirs.Count) && (absoluteDirs[use].Equals(relativeDirs[use], StringComparison.OrdinalIgnoreCase)))
				++use;
			return use == 0 ? absolutePath : string.Join("\\", relativeDirs.Skip(use).Select(str => "..").Concat(absoluteDirs.Skip(use)));
		}

		static string GetSize(string path)
		{
			if (File.Exists(path))
			{
				var fileinfo = new FileInfo(path);
				return fileinfo.Length.ToString();
			}

			if (Directory.Exists(path))
			{
				var dirs = new List<string> { path };
				for (var ctr = 0; ctr < dirs.Count; ++ctr)
					dirs.AddRange(Directory.EnumerateDirectories(dirs[ctr]));
				int files = 0;
				long totalSize = 0;
				foreach (var dir in dirs)
				{
					foreach (var file in Directory.EnumerateFiles(dir))
					{
						++files;
						var fileinfo = new FileInfo(file);
						totalSize += fileinfo.Length;
					}
				}

				return $"{dirs.Count} directories, {files} files, {totalSize} bytes";
			}

			return "INVALID";
		}

		static void ReencodeFile(string inputFile, IProgress<ProgressReport> progress, CancellationToken cancel, Coder.CodePage inputCodePage, Coder.CodePage outputCodePage)
		{
			if (cancel.IsCancellationRequested)
				return;

			var outputFile = Path.Combine(Path.GetDirectoryName(inputFile), Guid.NewGuid().ToString());
			try
			{
				using (var input = File.OpenRead(inputFile))
				{
					var bom = new byte[Coder.MaxPreambleSize];
					var headerSize = input.Read(bom, 0, (int)Math.Min(input.Length, bom.Length));
					Array.Resize(ref bom, headerSize);
					inputCodePage = Coder.ResolveCodePage(inputCodePage, bom);
					if (inputCodePage == outputCodePage)
						return;
					input.Position = Coder.PreambleSize(inputCodePage);

					using (var output = File.Create(outputFile))
					{
						bom = Coder.StringToBytes("", outputCodePage, true);
						output.Write(bom, 0, bom.Length);
						var decoder = Coder.GetEncoding(inputCodePage).GetDecoder();
						var encoder = Coder.GetEncoding(outputCodePage).GetEncoder();
						var chars = new char[65536];
						var bytes = new byte[chars.Length * 5]; // Should be big enough to hold any resulting output
						while (input.Position != input.Length)
						{
							if (cancel.IsCancellationRequested)
								throw new OperationCanceledException();

							var inByteCount = input.Read(bytes, 0, (int)Math.Min(chars.Length, input.Length - input.Position));

							var numChars = decoder.GetChars(bytes, 0, inByteCount, chars, 0);
							var outByteCount = encoder.GetBytes(chars, 0, numChars, bytes, 0, false);

							output.Write(bytes, 0, outByteCount);

							progress.Report(new ProgressReport(input.Position, input.Length));
						}
					}
				}
				File.Delete(inputFile);
				File.Move(outputFile, inputFile);
			}
			catch
			{
				File.Delete(outputFile);
				throw;
			}
		}

		static string RunCommand(string arguments, string workingDirectory)
		{
			var output = new StringBuilder();
			output.AppendLine($"Command: {arguments}");

			var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = "cmd.exe",
					Arguments = $"/c \"{arguments}\"",
					WorkingDirectory = workingDirectory,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
				},
			};
			process.OutputDataReceived += (s, e) => { if (e.Data != null) output.AppendLine(e.Data); };
			process.ErrorDataReceived += (s, e) => { if (e.Data != null) output.AppendLine($"Error: {e.Data}"); };
			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
			process.WaitForExit();
			return output.ToString();
		}

		static string SanitizeFileName(string fileName)
		{
			fileName = fileName.Trim();
			var start = "";
			if ((fileName.Length >= 2) && (fileName[1] == ':'))
				start = fileName.Substring(0, 2);
			fileName = fileName.Replace("/", @"\");
			fileName = Regex.Replace(fileName, "[<>:\"|?*\u0000-\u001f]", "_");
			fileName = start + fileName.Substring(start.Length);
			return fileName;
		}

		static void SetFileSize(string fileName, long value)
		{
			var fileInfo = new FileInfo(fileName);
			if (!fileInfo.Exists)
				throw new Exception($"File doesn't exist: {fileName}");

			value = Math.Max(0, value);

			if (fileInfo.Length == value)
				return;

			using (var file = File.Open(fileName, FileMode.Open))
				file.SetLength(value);
		}

		static async Task SplitFileAsync(string fileName, string outputTemplate, long chunkSize, IProgress<ProgressReport> progress, CancellationToken cancel)
		{
			if (chunkSize <= 0)
				throw new Exception($"Invalid chunk size: {chunkSize}");

			var chunk = 0;
			var buffer = new byte[65536];
			using (var inputFile = File.OpenRead(fileName))
				while (inputFile.Position < inputFile.Length)
					using (var outputFile = File.Create(string.Format(outputTemplate, ++chunk)))
					{
						var endChunk = Math.Min(inputFile.Position + chunkSize, inputFile.Length);
						while (inputFile.Position < endChunk)
						{
							if (cancel.IsCancellationRequested)
								break;

							var block = await inputFile.ReadAsync(buffer, 0, (int)Math.Min(buffer.Length, endChunk - inputFile.Position));
							if (block <= 0)
								throw new Exception("Failed to read file");
							progress.Report(new ProgressReport(inputFile.Position, inputFile.Length));
							await outputFile.WriteAsync(buffer, 0, block);
						}
					}
		}

		void Command_Files_Name_Simplify() => ReplaceSelections(Selections.Select(range => Path.GetFullPath(GetString(range))).ToList());

		FilesNamesMakeAbsoluteRelativeDialog.Result Command_Files_Name_MakeAbsolute_Dialog() => FilesNamesMakeAbsoluteRelativeDialog.Run(TabsParent, GetVariables(), true, true);

		void Command_Files_Name_MakeAbsolute(FilesNamesMakeAbsoluteRelativeDialog.Result result)
		{
			var results = GetExpressionResults<string>(result.Expression, Selections.Count());
			ReplaceSelections(GetSelectionStrings().Select((str, index) => new Uri(new Uri(results[index] + (result.Type == FilesNamesMakeAbsoluteRelativeDialog.ResultType.Directory ? "\\" : "")), str).LocalPath).ToList());
		}

		FilesNamesMakeAbsoluteRelativeDialog.Result Command_Files_Name_MakeRelative_Dialog() => FilesNamesMakeAbsoluteRelativeDialog.Run(TabsParent, GetVariables(), false, true);

		void Command_Files_Name_MakeRelative(FilesNamesMakeAbsoluteRelativeDialog.Result result)
		{
			var results = GetExpressionResults<string>(result.Expression, Selections.Count());
			if (result.Type == FilesNamesMakeAbsoluteRelativeDialog.ResultType.File)
				results = results.Select(str => Path.GetDirectoryName(str)).ToList();
			ReplaceSelections(GetSelectionStrings().Select((str, index) => GetRelativePath(str, results[index])).ToList());
		}

		FilesNamesGetUniqueDialog.Result Command_Files_Name_GetUnique_Dialog() => FilesNamesGetUniqueDialog.Run(TabsParent);

		void Command_Files_Name_GetUnique(FilesNamesGetUniqueDialog.Result result)
		{
			var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (!result.Format.Contains("{Unique}"))
				throw new Exception("Format must contain \"{Unique}\" tag");
			var newNames = new List<string>();
			var format = result.Format.Replace("{Path}", "{0}").Replace("{Name}", "{1}").Replace("{Unique}", "{2}").Replace("{Ext}", "{3}");
			foreach (var fileName in GetSelectionStrings())
			{
				var path = Path.GetDirectoryName(fileName);
				if (!string.IsNullOrEmpty(path))
					path += @"\";
				var name = Path.GetFileNameWithoutExtension(fileName);
				var ext = Path.GetExtension(fileName);
				var newFileName = fileName;
				for (var num = result.RenameAll ? 1 : 2; ; ++num)
				{
					if ((result.CheckExisting) && (Helpers.FileOrDirectoryExists(newFileName)))
						used.Add(newFileName);
					if (((num != 1) || (!result.RenameAll)) && (!used.Contains(newFileName)))
						break;
					var unique = result.UseGUIDs ? Guid.NewGuid().ToString() : num.ToString();

					newFileName = string.Format(format, path, name, unique, ext);
				}
				newNames.Add(newFileName);
				used.Add(newFileName);
			}

			ReplaceSelections(newNames);
		}

		void Command_Files_Name_Sanitize() => ReplaceSelections(Selections.Select(range => SanitizeFileName(GetString(range))).ToList());

		void Command_Files_Get_Size() => ReplaceSelections(RelativeSelectedFiles().Select(file => GetSize(file)).ToList());

		void Command_Files_Get_Time(TimestampType timestampType)
		{
			Func<FileSystemInfo, DateTime> getTime;
			switch (timestampType)
			{
				case TimestampType.Write: getTime = fi => fi.LastWriteTime; break;
				case TimestampType.Access: getTime = fi => fi.LastAccessTime; break;
				case TimestampType.Create: getTime = fi => fi.CreationTime; break;
				default: throw new Exception("Invalid TimestampType");
			}

			var files = RelativeSelectedFiles();
			var strs = new List<string>();
			foreach (var file in files)
			{
				if (File.Exists(file))
					strs.Add(getTime(new FileInfo(file)).ToString("yyyy-MM-dd HH:mm:ss.fff"));
				else if (Directory.Exists(file))
					strs.Add(getTime(new DirectoryInfo(file)).ToString("yyyy-MM-dd HH:mm:ss.fff"));
				else
					strs.Add("INVALID");
			}
			ReplaceSelections(strs);
		}

		void Command_Files_Get_Attributes()
		{
			var files = RelativeSelectedFiles();
			var strs = new List<string>();
			foreach (var file in files)
			{
				if (File.Exists(file))
				{
					var fileinfo = new FileInfo(file);
					strs.Add(fileinfo.Attributes.ToString());
				}
				else if (Directory.Exists(file))
				{
					var dirinfo = new DirectoryInfo(file);
					strs.Add(dirinfo.Attributes.ToString());
				}
				else
					strs.Add("INVALID");
			}
			ReplaceSelections(strs);
		}

		void Command_Files_Get_Version_File() => ReplaceSelections(RelativeSelectedFiles().AsParallel().AsOrdered().Select(file => FileVersionInfo.GetVersionInfo(file).FileVersion).ToList());

		void Command_Files_Get_Version_Product() => ReplaceSelections(RelativeSelectedFiles().AsParallel().AsOrdered().Select(file => FileVersionInfo.GetVersionInfo(file).ProductVersion).ToList());

		void Command_Files_Get_Version_Assembly() => ReplaceSelections(RelativeSelectedFiles().AsParallel().AsOrdered().Select(file => AssemblyName.GetAssemblyName(file).Version.ToString()).ToList());

		void Command_Files_Get_ChildrenDescendants(bool recursive)
		{
			var dirs = RelativeSelectedFiles();
			if (dirs.Any(dir => !Directory.Exists(dir)))
				throw new ArgumentException("Path must be of existing directories");

			var errors = new List<string>();
			ReplaceSelections(dirs.Select(dir => string.Join(Data.DefaultEnding, GetDirectoryContents(dir, recursive, errors))).ToList());
			if (errors.Any())
				Message.Show($"The following error(s) occurred:\n{string.Join("\n", errors)}", "Error", TabsParent);
		}

		void Command_Files_Get_VersionControlStatus()
		{
			var versioner = new Versioner();
			ReplaceSelections(RelativeSelectedFiles().Select(x => versioner.GetStatus(x).ToString()).ToList());
		}

		FilesSetSizeDialog.Result Command_Files_Set_Size_Dialog()
		{
			var vars = GetVariables();
			var sizes = RelativeSelectedFiles().AsParallel().AsOrdered().Select(file => new FileInfo(file).Length);
			vars.Add(NEVariable.List("size", "File size", () => sizes));
			return FilesSetSizeDialog.Run(TabsParent, vars);
		}

		void Command_Files_Set_Size(FilesSetSizeDialog.Result result)
		{
			var vars = GetVariables();
			var files = RelativeSelectedFiles();
			var sizes = files.AsParallel().AsOrdered().Select(file => new FileInfo(file).Length);
			vars.Add(NEVariable.List("size", "File size", () => sizes));
			var results = new NEExpression(result.Expression).EvaluateList<long>(vars, Selections.Count()).Select(size => size * result.Factor).ToList();
			files.Zip(results, (file, size) => new { file, size }).AsParallel().ForEach(obj => SetFileSize(obj.file, obj.size));
		}

		FilesSetTimeDialog.Result Command_Files_Set_Time_Dialog() => FilesSetTimeDialog.Run(TabsParent, GetVariables(), $@"""{DateTime.Now}""");

		void Command_Files_Set_Time(TimestampType type, FilesSetTimeDialog.Result result)
		{
			var dateTimes = GetExpressionResults<DateTime>(result.Expression, Selections.Count());
			var files = RelativeSelectedFiles();
			for (var ctr = 0; ctr < files.Count; ++ctr)
			{
				var dateTime = dateTimes[ctr];
				var file = files[ctr];
				if (!Helpers.FileOrDirectoryExists(file))
					File.WriteAllBytes(file, new byte[0]);

				if (File.Exists(file))
				{
					var info = new FileInfo(file);
					if (type.HasFlag(TimestampType.Write))
						info.LastWriteTime = dateTime;
					if (type.HasFlag(TimestampType.Access))
						info.LastAccessTime = dateTime;
					if (type.HasFlag(TimestampType.Create))
						info.CreationTime = dateTime;
				}
				else if (Directory.Exists(file))
				{
					var info = new DirectoryInfo(file);
					if (type.HasFlag(TimestampType.Write))
						info.LastWriteTime = dateTime;
					if (type.HasFlag(TimestampType.Access))
						info.LastAccessTime = dateTime;
					if (type.HasFlag(TimestampType.Create))
						info.CreationTime = dateTime;
				}
			}
		}

		FilesSetAttributesDialog.Result Command_Files_Set_Attributes_Dialog()
		{
			var filesAttrs = Selections.Select(range => GetString(range)).Select(file => new DirectoryInfo(file).Attributes).ToList();
			var availAttrs = Helpers.GetValues<FileAttributes>();
			var current = new Dictionary<FileAttributes, bool?>();
			foreach (var fileAttrs in filesAttrs)
				foreach (var availAttr in availAttrs)
				{
					var fileHasAttr = fileAttrs.HasFlag(availAttr);
					if (!current.ContainsKey(availAttr))
						current[availAttr] = fileHasAttr;
					if (current[availAttr] != fileHasAttr)
						current[availAttr] = null;
				}

			return FilesSetAttributesDialog.Run(TabsParent, current);
		}

		void Command_Files_Set_Attributes(FilesSetAttributesDialog.Result result)
		{
			FileAttributes andMask = 0, orMask = 0;
			foreach (var pair in result.Attributes)
			{
				andMask |= pair.Key;
				if ((pair.Value.HasValue) && (pair.Value.Value))
					orMask |= pair.Key;
			}
			foreach (var file in Selections.Select(range => GetString(range)))
				new FileInfo(file).Attributes = new FileInfo(file).Attributes & ~andMask | orMask;
		}

		FilesFindDialog.Result Command_Files_Find_Dialog() => FilesFindDialog.Run(TabsParent, GetVariables());

		void Command_Files_Find(FilesFindDialog.Result result)
		{
			// For each file, determine strings to find
			List<List<string>> stringsToFind;

			if (result.IsExpression)
			{
				var expressionResults = GetExpressionResults<string>(result.Text, result.AlignSelections ? Selections.Count : default(int?));
				if (result.AlignSelections)
					stringsToFind = Enumerable.Range(0, Selections.Count).Select(index => new List<string> { expressionResults[index] }).ToList();
				else
					stringsToFind = Enumerable.Repeat(expressionResults, Selections.Count).ToList();
			}
			else
				stringsToFind = Enumerable.Repeat(new List<string> { result.Text }, Selections.Count).ToList();

			List<bool> results;
			if (result.IsBinary)
			{
				var searchers = stringsToFind
					.Distinct()
					.ToDictionary(
						list => list,
						list => new BinarySearcher(list.SelectMany(
							item => result.CodePages
								.Select(codePage => (Coder.TryStringToBytes(item, codePage), (result.MatchCase) || (Coder.AlwaysCaseSensitive(codePage))))
								.Where(tuple => (tuple.Item1 != null) && (tuple.Item1.Length != 0))
							).Distinct().ToList()));

				results = MultiProgressDialog.RunAsync(TabsParent, "Searching files...", RelativeSelectedFiles().Select((file, index) => (file, index)), async (obj, progress, cancel) => await BinarySearchFileAsync(obj.file, searchers[stringsToFind[obj.index]], progress, cancel), obj => Path.GetFileName(obj.file));
			}
			else
			{
				// Create searchers
				Dictionary<List<string>, ISearcher> searchers;
				if (result.IsRegex)
				{
					searchers = stringsToFind
						.Distinct()
						.ToDictionary(
							list => list,
							list => new RegexesSearcher(list, matchCase: result.MatchCase, firstMatchOnly: true) as ISearcher);
				}
				else
				{
					searchers = stringsToFind
						.Distinct()
						.ToDictionary(
							list => list,
							list =>
							{
								if (list.Count == 1)
									return new StringSearcher(list[0], matchCase: result.MatchCase, firstMatchOnly: true) as ISearcher;
								return new StringsSearcher(list, matchCase: result.MatchCase, firstMatchOnly: true) as ISearcher;
							});
				}

				results = MultiProgressDialog.RunAsync(TabsParent, "Searching files...", RelativeSelectedFiles().Select((file, index) => (file, index)), async (obj, progress, cancel) => await TextSearchFileAsync(obj.file, searchers[stringsToFind[obj.index]], progress, cancel), obj => Path.GetFileName(obj.file));
			}

			SetSelections(Selections.Where((range, index) => results[index]).ToList());
		}

		FilesInsertDialog.Result Command_Files_Insert_Dialog() => FilesInsertDialog.Run(TabsParent);

		void Command_Files_Insert(FilesInsertDialog.Result result) => ReplaceSelections(RelativeSelectedFiles().AsParallel().AsOrdered().Select(fileName => Coder.BytesToString(File.ReadAllBytes(fileName), result.CodePage, true)).ToList());

		void Command_Files_Create_Files()
		{
			var files = RelativeSelectedFiles();
			if (files.Any(file => Directory.Exists(file)))
				throw new Exception("Directory already exists");
			files = files.Where(file => !File.Exists(file)).ToList();
			var data = new byte[0];
			foreach (var file in files)
				File.WriteAllBytes(file, data);
		}

		void Command_Files_Create_Directories()
		{
			var files = RelativeSelectedFiles();
			foreach (var file in files)
				Directory.CreateDirectory(file);
		}

		FilesCreateFromExpressionsDialog.Result Command_Files_Create_FromExpressions_Dialog() => FilesCreateFromExpressionsDialog.Run(TabsParent, GetVariables(), CodePage);

		void Command_Files_Create_FromExpressions(FilesCreateFromExpressionsDialog.Result result)
		{
			var variables = GetVariables();

			var filenameExpression = new NEExpression(result.FileName);
			var dataExpression = new NEExpression(result.Data);
			var resultCount = variables.ResultCount(filenameExpression, dataExpression);

			var filename = filenameExpression.EvaluateList<string>(variables, resultCount);
			var data = dataExpression.EvaluateList<string>(variables, resultCount);
			for (var ctr = 0; ctr < data.Count; ++ctr)
				File.WriteAllBytes(filename[ctr], Coder.StringToBytes(data[ctr], result.CodePage, true));
		}

		void Command_Files_Select_Name(GetPathType type) => SetSelections(Selections.AsParallel().AsOrdered().Select(range => GetPathRange(type, range)).ToList());

		void Command_Files_Select_Name_Next()
		{
			var maxPosition = Data.MaxPosition;
			var invalidChars = Path.GetInvalidFileNameChars();

			var sels = new List<Range>();
			foreach (var range in Selections)
			{
				var endPosition = range.End;
				while ((endPosition < maxPosition) && (((endPosition - range.Start == 1) && (Data.Data[endPosition] == ':')) || ((endPosition - range.End == 0) && ((Data.Data[endPosition] == '\\') || (Data.Data[endPosition] == '/'))) || (!invalidChars.Contains(Data.Data[endPosition]))))
					++endPosition;

				sels.Add(new Range(endPosition, range.Start));
			}

			SetSelections(sels);
		}

		void Command_Files_Select_Files() => SetSelections(Selections.Where(range => File.Exists(FileName.RelativeChild(GetString(range)))).ToList());

		void Command_Files_Select_Directories() => SetSelections(Selections.Where(range => Directory.Exists(FileName.RelativeChild(GetString(range)))).ToList());

		void Command_Files_Select_Existing(bool existing) => SetSelections(Selections.Where(range => Helpers.FileOrDirectoryExists(FileName.RelativeChild(GetString(range))) == existing).ToList());

		void Command_Files_Select_Roots(bool include)
		{
			var sels = Selections.Select(range => new { range = range, str = GetString(range).ToLower().Replace(@"\\", @"\").TrimEnd('\\') + @"\" }).ToList();
			var files = sels.Select(obj => obj.str).Distinct().OrderBy().ToList();
			var roots = new HashSet<string>();
			string root = null;
			foreach (var file in files)
			{
				if ((root != null) && (file.StartsWith(root)))
					continue;

				roots.Add(file);
				root = file;
			}

			SetSelections(sels.AsParallel().AsOrdered().Where(sel => roots.Contains(sel.str) == include).Select(sel => sel.range).ToList());
		}

		void Command_Files_Select_MatchDepth()
		{
			var strs = GetSelectionStrings();
			var minDepth = strs.Select(str => str.Count(c => c == '\\') + 1).DefaultIfEmpty(0).Min();
			SetSelections(Selections.Select((range, index) => Range.FromIndex(range.Start, GetDepthLength(strs[index], minDepth))).ToList());
		}

		void Command_Files_Select_CommonAncestor()
		{
			var strs = Selections.AsParallel().AsOrdered().Select(range => GetString(range) + "\\").ToList();
			var depth = 0;
			if (strs.Any())
			{
				var length = strs[0].Length;
				strs.Skip(1).ForEach(str => FindCommonLength(strs[0], str, ref length));
				depth = strs[0].Substring(0, length).Count(c => c == '\\');
			}
			SetSelections(Selections.Select((range, index) => Range.FromIndex(range.Start, GetDepthLength(strs[index], depth))).ToList());
		}

		FilesSelectByVersionControlStatusDialog.Result Command_Files_Select_ByVersionControlStatus_Dialog() => FilesSelectByVersionControlStatusDialog.Run(TabsParent);

		void Command_Files_Select_ByVersionControlStatus(FilesSelectByVersionControlStatusDialog.Result result)
		{
			var versioner = new Versioner();
			var statuses = RelativeSelectedFiles().Select(x => versioner.GetStatus(x)).ToList();
			var sels = Selections.Zip(statuses, (range, status) => new { range, status }).Where(obj => result.Statuses.HasFlag(obj.status)).Select(obj => obj.range).ToList();
			SetSelections(sels);
		}

		FilesHashDialog.Result Command_Files_Hash_Dialog() => FilesHashDialog.Run(TabsParent);

		void Command_Files_Hash(FilesHashDialog.Result result) => ReplaceSelections(MultiProgressDialog.RunAsync(TabsParent, "Calculating hashes...", RelativeSelectedFiles(), (file, progress, cancel) => Hasher.GetAsync(file, result.HashType, result.HMACKey, progress, cancel)));

		FilesSignDialog.Result Command_Files_Sign_Dialog() => FilesSignDialog.Run(TabsParent);

		void Command_Files_Sign(FilesSignDialog.Result result) => ReplaceSelections(RelativeSelectedFiles().Select(file => Cryptor.Sign(file, result.CryptorType, result.Key, result.Hash)).ToList());

		FilesOperationsCopyMoveDialog.Result Command_Files_Operations_CopyMove_Dialog(bool move) => FilesOperationsCopyMoveDialog.Run(TabsParent, GetVariables(), move);

		void Command_Files_Operations_CopyMove(FilesOperationsCopyMoveDialog.Result result, bool move)
		{
			var variables = GetVariables();

			var oldFileNameExpression = new NEExpression(result.OldFileName);
			var newFileNameExpression = new NEExpression(result.NewFileName);
			var resultCount = variables.ResultCount(oldFileNameExpression, newFileNameExpression);

			var oldFileNames = oldFileNameExpression.EvaluateList<string>(variables, resultCount);
			var newFileNames = newFileNameExpression.EvaluateList<string>(variables, resultCount);

			const int InvalidCount = 10;
			var invalid = oldFileNames.Distinct().Where(name => !Helpers.FileOrDirectoryExists(name)).Take(InvalidCount).ToList();
			if (invalid.Any())
				throw new Exception($"Sources don't exist:\n{string.Join("\n", invalid)}");

			invalid = newFileNames.Select(name => Path.GetDirectoryName(name)).Distinct().Where(dir => !Directory.Exists(dir)).Take(InvalidCount).ToList();
			if (invalid.Any())
				throw new Exception($"Directories don't exist:\n{string.Join("\n", invalid)}");

			// If user specified a file and a directory, assume they want the file (with the same name) in that directory
			newFileNames = newFileNames.Zip(oldFileNames, (newFileName, oldFileName) => (File.Exists(oldFileName)) && (Directory.Exists(newFileName)) ? Path.Combine(newFileName, Path.GetFileName(oldFileName)) : newFileName).ToList();

			invalid = oldFileNames.Zip(newFileNames, (oldFileName, newFileName) => new { oldFileName, newFileName }).Where(obj => (Directory.Exists(obj.newFileName)) || ((Directory.Exists(obj.oldFileName)) && (File.Exists(obj.newFileName)))).Select(pair => pair.newFileName).Distinct().Take(InvalidCount).ToList();
			if (invalid.Any())
				throw new Exception($"Destinations already exist:\n{string.Join("\n", invalid)}");

			var confirmCopyMove = $"{nameof(Command_Files_Operations_CopyMove)}_Confirm";
			if (!savedAnswers[confirmCopyMove].HasFlag(MessageOptions.All))
				savedAnswers[confirmCopyMove] = new Message(TabsParent)
				{
					Title = "Confirm",
					Text = $"Are you sure you want to {(move ? "move" : "copy")} these {resultCount} files/directories?",
					Options = MessageOptions.YesNoAll,
					DefaultAccept = MessageOptions.Yes,
					DefaultCancel = MessageOptions.No,
				}.Show();
			if (!savedAnswers[confirmCopyMove].HasFlag(MessageOptions.Yes))
				return;

			var overwriteCopyMove = $"{nameof(Command_Files_Operations_CopyMove)}_Overwrite";
			invalid = newFileNames.Zip(oldFileNames, (newFileName, oldFileName) => new { newFileName, oldFileName }).Where(obj => (!string.Equals(obj.newFileName, obj.oldFileName, StringComparison.OrdinalIgnoreCase)) && (File.Exists(obj.newFileName))).Select(obj => obj.newFileName).Distinct().Take(InvalidCount).ToList();
			if (invalid.Any())
			{
				if (!savedAnswers[overwriteCopyMove].HasFlag(MessageOptions.All))
					savedAnswers[overwriteCopyMove] = new Message(TabsParent)
					{
						Title = "Confirm",
						Text = $"Are you sure you want to overwrite these files:\n{string.Join("\n", invalid)}",
						Options = MessageOptions.YesNoAll,
						DefaultAccept = MessageOptions.Yes,
						DefaultCancel = MessageOptions.No,
					}.Show();
				if (!savedAnswers[overwriteCopyMove].HasFlag(MessageOptions.Yes))
					return;
			}

			for (var ctr = 0; ctr < oldFileNames.Count; ++ctr)
				if (Directory.Exists(oldFileNames[ctr]))
				{
					if (move)
						Directory.Move(oldFileNames[ctr], newFileNames[ctr]);
					else
						CopyDirectory(oldFileNames[ctr], newFileNames[ctr]);
				}
				else
				{
					if ((File.Exists(newFileNames[ctr])) && (!string.Equals(oldFileNames[ctr], newFileNames[ctr], StringComparison.OrdinalIgnoreCase)))
						File.Delete(newFileNames[ctr]);

					if (move)
						File.Move(oldFileNames[ctr], newFileNames[ctr]);
					else
						File.Copy(oldFileNames[ctr], newFileNames[ctr]);
				}
		}

		void Command_Files_Operations_Delete()
		{
			var files = RelativeSelectedFiles();
			if (!files.Any())
				return;

			var sureAnswer = $"{nameof(Command_Files_Operations_Delete)}_Sure";
			var continueAnswer = $"{nameof(Command_Files_Operations_Delete)}_Continue";

			if (!savedAnswers[sureAnswer].HasFlag(MessageOptions.All))
				savedAnswers[sureAnswer] = new Message(TabsParent)
				{
					Title = "Confirm",
					Text = "Are you sure you want to delete these files/directories?",
					Options = MessageOptions.YesNoAllCancel,
					DefaultAccept = MessageOptions.No,
					DefaultCancel = MessageOptions.Cancel,
				}.Show();
			if (!savedAnswers[sureAnswer].HasFlag(MessageOptions.Yes))
				return;

			foreach (var file in files)
			{
				try
				{
					if (File.Exists(file))
						File.Delete(file);
					if (Directory.Exists(file))
						Directory.Delete(file, true);
				}
				catch (Exception ex)
				{
					if (!savedAnswers[continueAnswer].HasFlag(MessageOptions.All))
						savedAnswers[continueAnswer] = new Message(TabsParent)
						{
							Title = "Confirm",
							Text = $"An error occurred:\n\n{ex.Message}\n\nContinue?",
							Options = MessageOptions.YesNoAll,
							DefaultAccept = MessageOptions.Yes,
							DefaultCancel = MessageOptions.No,
						}.Show();

					if (!savedAnswers[continueAnswer].HasFlag(MessageOptions.Yes))
						throw new OperationCanceledException();
				}
			}
		}

		void Command_Files_Operations_DragDrop()
		{
			var strs = RelativeSelectedFiles();
			if (!StringsAreFiles(strs))
				throw new Exception("Selections must be files.");
			DragFiles = strs;
		}

		void Command_Files_Operations_Explore()
		{
			if (Selections.Count != 1)
				throw new Exception("Can only explore one file.");
			Process.Start("explorer.exe", $"/select,\"{RelativeSelectedFiles()[0]}\"");
		}

		void Command_Files_Operations_CommandPrompt()
		{
			var dirs = RelativeSelectedFiles().Select(path => File.Exists(path) ? Path.GetDirectoryName(path) : path).Distinct().ToList();
			if (dirs.Count != 1)
				throw new Exception("Too many file locations.");
			Process.Start(new ProcessStartInfo("cmd.exe") { WorkingDirectory = dirs[0] });
		}

		void Command_Files_Operations_RunCommand_Parallel()
		{
			var workingDirectory = Path.GetDirectoryName(FileName ?? "");
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => RunCommand(GetString(range), workingDirectory)).ToList());
		}

		void Command_Files_Operations_RunCommand_Sequential() => ReplaceSelections(GetSelectionStrings().Select(str => RunCommand(str, Path.GetDirectoryName(FileName ?? ""))).ToList());

		void Command_Files_Operations_RunCommand_Shell() => GetSelectionStrings().ForEach(str => Process.Start(str));

		FilesOperationsEncodingDialog.Result Command_Files_Operations_Encoding_Dialog() => FilesOperationsEncodingDialog.Run(TabsParent);

		void Command_Files_Operations_Encoding(FilesOperationsEncodingDialog.Result result) => MultiProgressDialog.Run(TabsParent, "Changing encoding...", RelativeSelectedFiles(), (inputFile, progress, cancel) => ReencodeFile(inputFile, progress, cancel, result.InputCodePage, result.OutputCodePage));

		FilesOperationsSplitFileDialog.Result Command_Files_Operations_SplitFile_Dialog()
		{
			var variables = GetVariables();
			variables.Add(NEVariable.Constant("chunk", "Chunk number", 1));
			return FilesOperationsSplitFileDialog.Run(TabsParent, variables);
		}

		void Command_Files_Operations_SplitFile(FilesOperationsSplitFileDialog.Result result)
		{
			var variables = GetVariables();
			variables.Add(NEVariable.Constant("chunk", "Chunk number", "{0}"));
			var files = RelativeSelectedFiles();
			var outputTemplates = new NEExpression(result.OutputTemplate).EvaluateList<string>(variables, Selections.Count);
			var chunkSizes = new NEExpression(result.ChunkSize).EvaluateList<long>(variables, Selections.Count, "bytes");
			MultiProgressDialog.RunAsync(TabsParent, "Splitting files...", Enumerable.Range(0, Selections.Count), (index, progress, cancel) => SplitFileAsync(files[index], outputTemplates[index], chunkSizes[index], progress, cancel), index => Path.GetFileName(files[index]));
		}

		FilesOperationsCombineFilesDialog.Result Command_Files_Operations_CombineFiles_Dialog() => FilesOperationsCombineFilesDialog.Run(TabsParent, GetVariables());

		void Command_Files_Operations_CombineFiles(FilesOperationsCombineFilesDialog.Result result)
		{
			var variables = GetVariables();

			var inputFileCountExpr = new NEExpression(result.InputFileCount);
			var outputFilesExpr = new NEExpression(result.OutputFiles);
			var count = variables.ResultCount(inputFileCountExpr, outputFilesExpr);

			var inputFiles = new NEExpression(result.InputFiles).EvaluateList<string>(variables);
			var inputFileCount = inputFileCountExpr.EvaluateList<int>(variables, count);
			var outputFiles = outputFilesExpr.EvaluateList<string>(variables, count);

			if (inputFiles.Count != inputFileCount.Sum())
				throw new Exception("Invalid input file count");

			var current = -1;
			var inputs = new List<List<string>>();
			foreach (var inputFile in inputFiles)
			{
				while ((current < 0) || (inputs[current].Count == inputFileCount[current]))
				{
					++current;
					inputs.Add(new List<string>());
				}

				inputs[current].Add(inputFile);
			}

			MultiProgressDialog.RunAsync(TabsParent, "Combining files...", Enumerable.Range(0, outputFiles.Count), (index, progress, cancel) => CombineFilesAsync(outputFiles[index], inputs[index], progress, cancel), index => Path.GetFileName(outputFiles[index]));
		}
	}
}
