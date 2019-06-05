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
using NeoEdit.Common;
using NeoEdit.Common.Controls;
using NeoEdit.Common.Dialogs;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.Dialogs;

namespace NeoEdit
{
	public static class FilesFunctions
	{
		static public void Load() { } // Doesn't do anything except load the assembly

		static async Task<bool> BinarySearchFileAsync(ITextEditor te, string fileName, Searcher searcher, AnswerResult answer, IProgress<ProgressReport> progress, CancellationToken cancel)
		{
			try
			{
				if (answer.Answer == MessageOptions.Cancel)
					return true;

				if (Directory.Exists(fileName))
					return false;

				var findLen = searcher.MaxLen;
				if (findLen == 0)
					return false;
				var buffer = new byte[8192];
				var used = 0;
				using (var stream = File.OpenRead(fileName))
					while (!cancel.IsCancellationRequested)
					{
						var block = await stream.ReadAsync(buffer, used, buffer.Length - used);
						if (block == 0)
							break;
						used += block;
						progress.Report(new ProgressReport(stream.Position, stream.Length));

						var result = searcher.Find(buffer, 0, used, true);
						if (result.Any())
							return true;

						var keep = Math.Min(used, findLen - 1);
						Array.Copy(buffer, used - keep, buffer, 0, keep);
						used = keep;
					}

				return false;
			}
			catch (Exception ex)
			{
				te.TabsParent.Dispatcher.Invoke(() =>
				{
					if ((answer.Answer != MessageOptions.YesToAll) && (answer.Answer != MessageOptions.NoToAll))
						answer.Answer = new Message(te.WindowParent)
						{
							Title = "Confirm",
							Text = $"Unable to read {fileName}.\n\n{ex.Message}\n\nLeave selected?",
							Options = MessageOptions.YesNoYesAllNoAllCancel,
							DefaultCancel = MessageOptions.Cancel,
						}.Show();
				});

				return (answer.Answer == MessageOptions.Yes) || (answer.Answer == MessageOptions.YesToAll) || (answer.Answer == MessageOptions.Cancel);
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

		static Range GetPathRange(ITextEditor te, GetPathType type, Range range)
		{
			var path = te.GetString(range);
			var dirLength = Math.Max(0, path.LastIndexOf('\\'));
			if ((path.StartsWith(@"\\")) && (dirLength == 1))
				dirLength = 0;
			var dirTotal = dirLength == 0 ? 0 : dirLength + 1;
			var extLen = Path.GetExtension(path).Length;

			switch (type)
			{
				case GetPathType.FileName: return new Range(range.End, range.Start + dirTotal);
				case GetPathType.FileNameWoExtension: return new Range(range.End - extLen, range.Start + dirTotal);
				case GetPathType.Directory: return new Range(range.Start + dirLength, range.Start);
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

		static async Task<bool> TextSearchFileAsync(ITextEditor te, string fileName, FilesFindTextDialog.Result search, AnswerResult answer, IProgress<ProgressReport> progress, CancellationToken cancel)
		{
			try
			{
				if (answer.Answer == MessageOptions.Cancel)
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
				var start = data.GetOffset(0, 0);
				return data.RegexMatches(search.Regex, start, data.NumChars - start, search.MultiLine, false, true).Any();
			}
			catch (Exception ex)
			{
				te.TabsParent.Dispatcher.Invoke(() =>
				{
					if ((answer.Answer != MessageOptions.YesToAll) && (answer.Answer != MessageOptions.NoToAll))
						answer.Answer = new Message(te.WindowParent)
						{
							Title = "Confirm",
							Text = $"Unable to read {fileName}.\n\n{ex.Message}\n\nLeave selected?",
							Options = MessageOptions.YesNoYesAllNoAllCancel,
							DefaultCancel = MessageOptions.Cancel,
						}.Show();
				});

				return (answer.Answer == MessageOptions.Yes) || (answer.Answer == MessageOptions.YesToAll) || (answer.Answer == MessageOptions.Cancel);
			}
		}

		static public void Command_Files_Name_Simplify(ITextEditor te) => te.ReplaceSelections(te.Selections.Select(range => Path.GetFullPath(te.GetString(range))).ToList());

		static public FilesNamesMakeAbsoluteRelativeDialog.Result Command_Files_Name_MakeAbsolute_Dialog(ITextEditor te) => FilesNamesMakeAbsoluteRelativeDialog.Run(te.WindowParent, te.GetVariables(), true, true);

		static public void Command_Files_Name_MakeAbsolute(ITextEditor te, FilesNamesMakeAbsoluteRelativeDialog.Result result)
		{
			var results = te.GetFixedExpressionResults<string>(result.Expression);
			te.ReplaceSelections(te.GetSelectionStrings().Select((str, index) => new Uri(new Uri(results[index] + (result.Type == FilesNamesMakeAbsoluteRelativeDialog.ResultType.Directory ? "\\" : "")), str).LocalPath).ToList());
		}

		static public FilesNamesMakeAbsoluteRelativeDialog.Result Command_Files_Name_MakeRelative_Dialog(ITextEditor te) => FilesNamesMakeAbsoluteRelativeDialog.Run(te.WindowParent, te.GetVariables(), false, true);

		static public void Command_Files_Name_MakeRelative(ITextEditor te, FilesNamesMakeAbsoluteRelativeDialog.Result result)
		{
			var results = te.GetFixedExpressionResults<string>(result.Expression);
			if (result.Type == FilesNamesMakeAbsoluteRelativeDialog.ResultType.File)
				results = results.Select(str => Path.GetDirectoryName(str)).ToList();
			te.ReplaceSelections(te.GetSelectionStrings().Select((str, index) => GetRelativePath(str, results[index])).ToList());
		}

		static public FilesNamesGetUniqueDialog.Result Command_Files_Name_GetUnique_Dialog(ITextEditor te) => FilesNamesGetUniqueDialog.Run(te.WindowParent);

		static public void Command_Files_Name_GetUnique(ITextEditor te, FilesNamesGetUniqueDialog.Result result)
		{
			var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (!result.Format.Contains("{Unique}"))
				throw new Exception("Format must contain \"{Unique}\" tag");
			var newNames = new List<string>();
			var format = result.Format.Replace("{Path}", "{0}").Replace("{Name}", "{1}").Replace("{Unique}", "{2}").Replace("{Ext}", "{3}");
			foreach (var fileName in te.GetSelectionStrings())
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

			te.ReplaceSelections(newNames);
		}

		static public void Command_Files_Name_Sanitize(ITextEditor te) => te.ReplaceSelections(te.Selections.Select(range => SanitizeFileName(te.GetString(range))).ToList());

		static public void Command_Files_Get_Size(ITextEditor te) => te.ReplaceSelections(te.RelativeSelectedFiles().Select(file => GetSize(file)).ToList());

		static public void Command_Files_Get_Time(ITextEditor te, TimestampType timestampType)
		{
			Func<FileSystemInfo, DateTime> getTime;
			switch (timestampType)
			{
				case TimestampType.Write: getTime = fi => fi.LastWriteTime; break;
				case TimestampType.Access: getTime = fi => fi.LastAccessTime; break;
				case TimestampType.Create: getTime = fi => fi.CreationTime; break;
				default: throw new Exception("Invalid TimestampType");
			}

			var files = te.RelativeSelectedFiles();
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
			te.ReplaceSelections(strs);
		}

		static public void Command_Files_Get_Attributes(ITextEditor te)
		{
			var files = te.RelativeSelectedFiles();
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
			te.ReplaceSelections(strs);
		}

		static public void Command_Files_Get_Version_File(ITextEditor te) => te.ReplaceSelections(te.RelativeSelectedFiles().AsParallel().AsOrdered().Select(file => FileVersionInfo.GetVersionInfo(file).FileVersion).ToList());

		static public void Command_Files_Get_Version_Product(ITextEditor te) => te.ReplaceSelections(te.RelativeSelectedFiles().AsParallel().AsOrdered().Select(file => FileVersionInfo.GetVersionInfo(file).ProductVersion).ToList());

		static public void Command_Files_Get_Version_Assembly(ITextEditor te) => te.ReplaceSelections(te.RelativeSelectedFiles().AsParallel().AsOrdered().Select(file => AssemblyName.GetAssemblyName(file).Version.ToString()).ToList());

		static public void Command_Files_Get_ChildrenDescendants(ITextEditor te, bool recursive)
		{
			var dirs = te.RelativeSelectedFiles();
			if (dirs.Any(dir => !Directory.Exists(dir)))
				throw new ArgumentException("Path must be of existing directories");

			var errors = new List<string>();
			te.ReplaceSelections(dirs.Select(dir => string.Join(te.Data.DefaultEnding, GetDirectoryContents(dir, recursive, errors))).ToList());
			if (errors.Any())
				Message.Show($"The following error(s) occurred:\n{string.Join("\n", errors)}", "Error", te.WindowParent);
		}

		static public void Command_Files_Get_VersionControlStatus(ITextEditor te)
		{
			var versioner = new Versioner();
			te.ReplaceSelections(te.RelativeSelectedFiles().Select(x => versioner.GetStatus(x).ToString()).ToList());
		}

		static public FilesSetSizeDialog.Result Command_Files_Set_Size_Dialog(ITextEditor te)
		{
			var vars = te.GetVariables();
			var sizes = te.RelativeSelectedFiles().AsParallel().AsOrdered().Select(file => new FileInfo(file).Length);
			vars.Add(NEVariable.List("size", "File size", () => sizes));
			return FilesSetSizeDialog.Run(te.WindowParent, vars);
		}

		static public void Command_Files_Set_Size(ITextEditor te, FilesSetSizeDialog.Result result)
		{
			var vars = te.GetVariables();
			var files = te.RelativeSelectedFiles();
			var sizes = files.AsParallel().AsOrdered().Select(file => new FileInfo(file).Length);
			vars.Add(NEVariable.List("size", "File size", () => sizes));
			var results = new NEExpression(result.Expression).EvaluateList<long>(vars, te.Selections.Count()).Select(size => size * result.Factor).ToList();
			files.Zip(results, (file, size) => new { file, size }).AsParallel().ForEach(obj => SetFileSize(obj.file, obj.size));
		}

		static public FilesSetTimeDialog.Result Command_Files_Set_Time_Dialog(ITextEditor te) => FilesSetTimeDialog.Run(te.WindowParent, te.GetVariables(), $@"""{DateTime.Now}""");

		static public void Command_Files_Set_Time(ITextEditor te, TimestampType type, FilesSetTimeDialog.Result result)
		{
			var dateTimes = te.GetFixedExpressionResults<DateTime>(result.Expression);
			var files = te.RelativeSelectedFiles();
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

		static public FilesSetAttributesDialog.Result Command_Files_Set_Attributes_Dialog(ITextEditor te)
		{
			var filesAttrs = te.Selections.Select(range => te.GetString(range)).Select(file => new DirectoryInfo(file).Attributes).ToList();
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

			return FilesSetAttributesDialog.Run(te.WindowParent, current);
		}

		static public void Command_Files_Set_Attributes(ITextEditor te, FilesSetAttributesDialog.Result result)
		{
			FileAttributes andMask = 0, orMask = 0;
			foreach (var pair in result.Attributes)
			{
				andMask |= pair.Key;
				if ((pair.Value.HasValue) && (pair.Value.Value))
					orMask |= pair.Key;
			}
			foreach (var file in te.Selections.Select(range => te.GetString(range)))
				new FileInfo(file).Attributes = new FileInfo(file).Attributes & ~andMask | orMask;
		}

		static public FilesFindBinaryDialog.Result Command_Files_Find_Binary_Dialog(ITextEditor te) => FilesFindBinaryDialog.Run(te.WindowParent);

		static public void Command_Files_Find_Binary(ITextEditor te, FilesFindBinaryDialog.Result result, AnswerResult answer)
		{
			var selected = te.RelativeSelectedFiles().Zip(te.Selections, (fileName, range) => new { fileName, range }).ToList();
			var searcher = Helpers.GetSearcher(new List<string> { result.Text }, result.CodePages, result.MatchCase);
			te.SetSelections(MultiProgressDialog.RunAsync(te.WindowParent, "Searching files...", selected, async (obj, progress, cancel) => await BinarySearchFileAsync(te, obj.fileName, searcher, answer, progress, cancel) ? obj.range : null, obj => Path.GetFileName(obj.fileName)).NonNull().ToList());
		}

		static public FilesFindTextDialog.Result Command_Files_Find_Text_Dialog(ITextEditor te) => FilesFindTextDialog.Run(te.WindowParent);

		static public void Command_Files_Find_Text(ITextEditor te, FilesFindTextDialog.Result result, AnswerResult answer)
		{
			var selected = te.RelativeSelectedFiles().Zip(te.Selections, (fileName, range) => new { fileName, range }).ToList();
			te.SetSelections(MultiProgressDialog.RunAsync(te.WindowParent, "Searching files...", selected, async (obj, progress, cancel) => await TextSearchFileAsync(te, obj.fileName, result, answer, progress, cancel) ? obj.range : null, obj => Path.GetFileName(obj.fileName)).NonNull().ToList());
		}

		static public FilesFindMassFindDialog.Result Command_Files_Find_MassFind_Dialog(ITextEditor te) => FilesFindMassFindDialog.Run(te.WindowParent, te.GetVariables());

		static public void Command_Files_Find_MassFind(ITextEditor te, FilesFindMassFindDialog.Result result, AnswerResult answer)
		{
			var findStrs = te.GetVariableExpressionResults<string>(result.Expression);
			var searcher = Helpers.GetSearcher(findStrs, result.CodePages, result.MatchCase);
			var selected = te.RelativeSelectedFiles().Zip(te.Selections, (fileName, range) => new { fileName, range }).ToList();
			te.SetSelections(MultiProgressDialog.RunAsync(te.WindowParent, "Searching files...", selected, async (obj, progress, cancel) => await BinarySearchFileAsync(te, obj.fileName, searcher, answer, progress, cancel) ? obj.range : null, obj => Path.GetFileName(obj.fileName)).NonNull().ToList());
		}

		static public FilesInsertDialog.Result Command_Files_Insert_Dialog(ITextEditor te) => FilesInsertDialog.Run(te.WindowParent);

		static public void Command_Files_Insert(ITextEditor te, FilesInsertDialog.Result result) => te.ReplaceSelections(te.RelativeSelectedFiles().AsParallel().AsOrdered().Select(fileName => Coder.BytesToString(File.ReadAllBytes(fileName), result.CodePage, true)).ToList());

		static public void Command_Files_Create_Files(ITextEditor te)
		{
			var files = te.RelativeSelectedFiles();
			if (files.Any(file => Directory.Exists(file)))
				throw new Exception("Directory already exists");
			files = files.Where(file => !File.Exists(file)).ToList();
			var data = new byte[0];
			foreach (var file in files)
				File.WriteAllBytes(file, data);
		}

		static public void Command_Files_Create_Directories(ITextEditor te)
		{
			var files = te.RelativeSelectedFiles();
			foreach (var file in files)
				Directory.CreateDirectory(file);
		}

		static public FilesCreateFromExpressionsDialog.Result Command_Files_Create_FromExpressions_Dialog(ITextEditor te) => FilesCreateFromExpressionsDialog.Run(te.WindowParent, te.GetVariables(), te.CodePage);

		static public void Command_Files_Create_FromExpressions(ITextEditor te, FilesCreateFromExpressionsDialog.Result result)
		{
			var variables = te.GetVariables();

			var filenameExpression = new NEExpression(result.FileName);
			var dataExpression = new NEExpression(result.Data);
			var resultCount = variables.ResultCount(filenameExpression, dataExpression);

			var filename = filenameExpression.EvaluateList<string>(variables, resultCount);
			var data = dataExpression.EvaluateList<string>(variables, resultCount);
			for (var ctr = 0; ctr < data.Count; ++ctr)
				File.WriteAllBytes(filename[ctr], Coder.StringToBytes(data[ctr], result.CodePage, true));
		}

		static public void Command_Files_Select_Name(ITextEditor te, GetPathType type) => te.SetSelections(te.Selections.AsParallel().AsOrdered().Select(range => GetPathRange(te, type, range)).ToList());

		static public void Command_Files_Select_Files(ITextEditor te) => te.SetSelections(te.Selections.Where(range => File.Exists(te.FileName.RelativeChild(te.GetString(range)))).ToList());

		static public void Command_Files_Select_Directories(ITextEditor te) => te.SetSelections(te.Selections.Where(range => Directory.Exists(te.FileName.RelativeChild(te.GetString(range)))).ToList());

		static public void Command_Files_Select_Existing(ITextEditor te, bool existing) => te.SetSelections(te.Selections.Where(range => Helpers.FileOrDirectoryExists(te.FileName.RelativeChild(te.GetString(range))) == existing).ToList());

		static public void Command_Files_Select_Roots(ITextEditor te, bool include)
		{
			var sels = te.Selections.Select(range => new { range = range, str = te.GetString(range).ToLower().Replace(@"\\", @"\").TrimEnd('\\') + @"\" }).ToList();
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

			te.SetSelections(sels.AsParallel().AsOrdered().Where(sel => roots.Contains(sel.str) == include).Select(sel => sel.range).ToList());
		}

		static public void Command_Files_Select_MatchDepth(ITextEditor te)
		{
			var strs = te.GetSelectionStrings();
			var minDepth = strs.Select(str => str.Count(c => c == '\\') + 1).DefaultIfEmpty(0).Min();
			te.SetSelections(te.Selections.Select((range, index) => Range.FromIndex(range.Start, GetDepthLength(strs[index], minDepth))).ToList());
		}

		static public void Command_Files_Select_CommonAncestor(ITextEditor te)
		{
			var strs = te.Selections.AsParallel().AsOrdered().Select(range => te.GetString(range) + "\\").ToList();
			var depth = 0;
			if (strs.Any())
			{
				var length = strs[0].Length;
				strs.Skip(1).ForEach(str => FindCommonLength(strs[0], str, ref length));
				depth = strs[0].Substring(0, length).Count(c => c == '\\');
			}
			te.SetSelections(te.Selections.Select((range, index) => Range.FromIndex(range.Start, GetDepthLength(strs[index], depth))).ToList());
		}

		static public FilesSelectByVersionControlStatusDialog.Result Command_Files_Select_ByVersionControlStatus_Dialog(ITextEditor te) => FilesSelectByVersionControlStatusDialog.Run(te.WindowParent);

		static public void Command_Files_Select_ByVersionControlStatus(ITextEditor te, FilesSelectByVersionControlStatusDialog.Result result)
		{
			var versioner = new Versioner();
			var statuses = te.RelativeSelectedFiles().Select(x => versioner.GetStatus(x)).ToList();
			var sels = te.Selections.Zip(statuses, (range, status) => new { range, status }).Where(obj => result.Statuses.HasFlag(obj.status)).Select(obj => obj.range).ToList();
			te.SetSelections(sels);
		}

		static public FilesHashDialog.Result Command_Files_Hash_Dialog(ITextEditor te) => FilesHashDialog.Run(te.WindowParent);

		static public void Command_Files_Hash(ITextEditor te, FilesHashDialog.Result result) => te.ReplaceSelections(MultiProgressDialog.RunAsync(te.WindowParent, "Calculating hashes...", te.RelativeSelectedFiles(), (file, progress, cancel) => Hasher.GetAsync(file, result.HashType, result.HMACKey, progress, cancel)));

		static public FilesSignDialog.Result Command_Files_Sign_Dialog(ITextEditor te) => FilesSignDialog.Run(te.WindowParent);

		static public void Command_Files_Sign(ITextEditor te, FilesSignDialog.Result result) => te.ReplaceSelections(te.RelativeSelectedFiles().Select(file => Cryptor.Sign(file, result.CryptorType, result.Key, result.Hash)).ToList());

		static public FilesOperationsCopyMoveDialog.Result Command_Files_Operations_CopyMove_Dialog(ITextEditor te, bool move) => FilesOperationsCopyMoveDialog.Run(te.WindowParent, te.GetVariables(), move);

		static public void Command_Files_Operations_CopyMove(ITextEditor te, FilesOperationsCopyMoveDialog.Result result, bool move)
		{
			var variables = te.GetVariables();

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

			if (new Message(te.WindowParent)
			{
				Title = "Confirm",
				Text = $"Are you sure you want to {(move ? "move" : "copy")} these {resultCount} files/directories?",
				Options = MessageOptions.YesNo,
				DefaultAccept = MessageOptions.Yes,
				DefaultCancel = MessageOptions.No,
			}.Show() != MessageOptions.Yes)
				return;

			invalid = newFileNames.Zip(oldFileNames, (newFileName, oldFileName) => new { newFileName, oldFileName }).Where(obj => (!string.Equals(obj.newFileName, obj.oldFileName, StringComparison.OrdinalIgnoreCase)) && (File.Exists(obj.newFileName))).Select(obj => obj.newFileName).Distinct().Take(InvalidCount).ToList();
			if (invalid.Any())
			{
				if (new Message(te.WindowParent)
				{
					Title = "Confirm",
					Text = $"Are you sure you want to overwrite these files:\n{string.Join("\n", invalid)}",
					Options = MessageOptions.YesNo,
					DefaultAccept = MessageOptions.Yes,
					DefaultCancel = MessageOptions.No,
				}.Show() != MessageOptions.Yes)
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

		static public void Command_Files_Operations_Delete(ITextEditor te)
		{
			if (new Message(te.WindowParent)
			{
				Title = "Confirm",
				Text = "Are you sure you want to delete these files/directories?",
				Options = MessageOptions.YesNo,
				DefaultCancel = MessageOptions.No,
			}.Show() != MessageOptions.Yes)
				return;

			var files = te.RelativeSelectedFiles();
			var answer = MessageOptions.None;
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
					if (answer != MessageOptions.YesToAll)
						answer = new Message(te.WindowParent)
						{
							Title = "Confirm",
							Text = $"An error occurred:\n\n{ex.Message}\n\nContinue?",
							Options = MessageOptions.YesNoYesAll,
							DefaultAccept = MessageOptions.Yes,
							DefaultCancel = MessageOptions.No,
						}.Show();

					if (answer == MessageOptions.No)
						break;
				}
			}
		}

		static public void Command_Files_Operations_DragDrop(ITextEditor te)
		{
			var strs = te.RelativeSelectedFiles();
			if (!te.StringsAreFiles(strs))
				throw new Exception("Selections must be files.");
			te.DragFiles = strs;
		}

		static public void Command_Files_Operations_Explore(ITextEditor te)
		{
			if (te.Selections.Count != 1)
				throw new Exception("Can only explore one file.");
			Process.Start("explorer.exe", $"/select,\"{te.RelativeSelectedFiles()[0]}\"");
		}

		static public void Command_Files_Operations_CommandPrompt(ITextEditor te)
		{
			var dirs = te.RelativeSelectedFiles().Select(path => File.Exists(path) ? Path.GetDirectoryName(path) : path).Distinct().ToList();
			if (dirs.Count != 1)
				throw new Exception("Too many file locations.");
			Process.Start(new ProcessStartInfo("cmd.exe") { WorkingDirectory = dirs[0] });
		}

		static public void Command_Files_Operations_RunCommand_Parallel(ITextEditor te)
		{
			var workingDirectory = Path.GetDirectoryName(te.FileName ?? "");
			te.ReplaceSelections(te.Selections.AsParallel().AsOrdered().Select(range => RunCommand(te.GetString(range), workingDirectory)).ToList());
		}

		static public void Command_Files_Operations_RunCommand_Sequential(ITextEditor te) => te.ReplaceSelections(te.GetSelectionStrings().Select(str => RunCommand(str, Path.GetDirectoryName(te.FileName ?? ""))).ToList());

		static public void Command_Files_Operations_RunCommand_Shell(ITextEditor te) => te.GetSelectionStrings().ForEach(str => Process.Start(str));

		static public FilesOperationsEncodingDialog.Result Command_Files_Operations_Encoding_Dialog(ITextEditor te) => FilesOperationsEncodingDialog.Run(te.WindowParent);

		static public void Command_Files_Operations_Encoding(ITextEditor te, FilesOperationsEncodingDialog.Result result) => MultiProgressDialog.Run(te.WindowParent, "Changing encoding...", te.RelativeSelectedFiles(), (inputFile, progress, cancel) => ReencodeFile(inputFile, progress, cancel, result.InputCodePage, result.OutputCodePage));

		static public FilesOperationsSplitFileDialog.Result Command_Files_Operations_SplitFile_Dialog(ITextEditor te)
		{
			var variables = te.GetVariables();
			variables.Add(NEVariable.Constant("chunk", "Chunk number", 1));
			return FilesOperationsSplitFileDialog.Run(te.WindowParent, variables);
		}

		static public void Command_Files_Operations_SplitFile(ITextEditor te, FilesOperationsSplitFileDialog.Result result)
		{
			var variables = te.GetVariables();
			variables.Add(NEVariable.Constant("chunk", "Chunk number", "{0}"));
			var files = te.RelativeSelectedFiles();
			var outputTemplates = new NEExpression(result.OutputTemplate).EvaluateList<string>(variables, te.Selections.Count);
			var chunkSizes = new NEExpression(result.ChunkSize).EvaluateList<long>(variables, te.Selections.Count, "bytes");
			MultiProgressDialog.RunAsync(te.WindowParent, "Splitting files...", Enumerable.Range(0, te.Selections.Count), (index, progress, cancel) => SplitFileAsync(files[index], outputTemplates[index], chunkSizes[index], progress, cancel), index => Path.GetFileName(files[index]));
		}

		static public FilesOperationsCombineFilesDialog.Result Command_Files_Operations_CombineFiles_Dialog(ITextEditor te) => FilesOperationsCombineFilesDialog.Run(te.WindowParent, te.GetVariables());

		static public void Command_Files_Operations_CombineFiles(ITextEditor te, FilesOperationsCombineFilesDialog.Result result)
		{
			var variables = te.GetVariables();

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

			MultiProgressDialog.RunAsync(te.WindowParent, "Combining files...", Enumerable.Range(0, outputFiles.Count), (index, progress, cancel) => CombineFilesAsync(outputFiles[index], inputs[index], progress, cancel), index => Path.GetFileName(outputFiles[index]));
		}
	}
}
