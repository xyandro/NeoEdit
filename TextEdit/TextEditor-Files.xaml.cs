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
using NeoEdit.Common;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.GUI;
using NeoEdit.GUI.Dialogs;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit
{
	partial class TextEditor
	{
		bool BinarySearchFile(string fileName, Searcher searcher, ref Message.OptionsEnum answer)
		{
			try
			{
				if (answer == Message.OptionsEnum.Cancel)
					return true;

				var findLen = searcher.MaxLen;
				if (findLen == 0)
					return false;
				var buffer = new byte[8192];
				var used = 0;
				using (var stream = File.OpenRead(fileName))
					while (true)
					{
						var block = stream.Read(buffer, used, buffer.Length - used);
						if (block == 0)
							break;
						used += block;

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
				if ((answer != Message.OptionsEnum.YesToAll) && (answer != Message.OptionsEnum.NoToAll))
					answer = new Message(WindowParent)
					{
						Title = "Confirm",
						Text = $"Unable to read {fileName}.\n\n{ex.Message}\n\nLeave selected?",
						Options = Message.OptionsEnum.YesNoYesAllNoAllCancel,
						DefaultCancel = Message.OptionsEnum.Cancel,
					}.Show();

				return (answer == Message.OptionsEnum.Yes) || (answer == Message.OptionsEnum.YesToAll) || (answer == Message.OptionsEnum.Cancel);
			}
		}

		void CopyDirectory(string src, string dest)
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

		int GetDepthLength(string path, int matchDepth)
		{
			var depth = 0;
			for (var index = 0; index < path.Length; ++index)
				if (path[index] == '\\')
					if (++depth == matchDepth)
						return index;
			return path.Length;
		}

		List<string> GetDirectoryContents(string dir, bool recursive, List<string> errors)
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
				case GetPathType.Directory: return new Range(range.Start + dirLength, range.Start);
				case GetPathType.Extension: return new Range(range.End, range.End - extLen);
				default: throw new ArgumentException();
			}
		}

		string GetRelativePath(string absolutePath, string relativeDirectory)
		{
			var absoluteDirs = absolutePath.Split('\\').ToList();
			var relativeDirs = relativeDirectory.Split('\\').ToList();
			var use = 0;
			while ((use < absoluteDirs.Count) && (use < relativeDirs.Count) && (absoluteDirs[use].Equals(relativeDirs[use], StringComparison.OrdinalIgnoreCase)))
				++use;
			return use == 0 ? absolutePath : string.Join("\\", relativeDirs.Skip(use).Select(str => "..").Concat(absoluteDirs.Skip(use)));
		}

		string GetSize(string path)
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

		void ReencodeFile(string inputFile, IProgress<ProgressReport> progress, CancellationToken cancel, Coder.CodePage inputCodePage, Coder.CodePage outputCodePage)
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

		string RunCommand(string arguments)
		{
			var output = new StringBuilder();
			output.AppendLine($"Command: {arguments}");

			var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = "cmd.exe",
					Arguments = $"/c \"{arguments}\"",
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

		string SanitizeFileName(string fileName)
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

		void SetFileSize(string fileName, long value)
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

		bool TextSearchFile(string fileName, FindTextDialog.Result search, ref Message.OptionsEnum answer)
		{
			try
			{
				if (answer == Message.OptionsEnum.Cancel)
					return true;

				var data = new TextData(Coder.BytesToString(File.ReadAllBytes(fileName), Coder.CodePage.AutoByBOM, true));
				var start = data.GetOffset(0, 0);
				return data.RegexMatches(search.Regex, start, data.NumChars - start, search.MultiLine, false, true).Any();
			}
			catch (Exception ex)
			{
				if ((answer != Message.OptionsEnum.YesToAll) && (answer != Message.OptionsEnum.NoToAll))
					answer = new Message(WindowParent)
					{
						Title = "Confirm",
						Text = $"Unable to read {fileName}.\n\n{ex.Message}\n\nLeave selected?",
						Options = Message.OptionsEnum.YesNoYesAllNoAllCancel,
						DefaultCancel = Message.OptionsEnum.Cancel,
					}.Show();

				return (answer == Message.OptionsEnum.Yes) || (answer == Message.OptionsEnum.YesToAll) || (answer == Message.OptionsEnum.Cancel);
			}
		}

		void Command_Files_Name_Simplify() => ReplaceSelections(Selections.Select(range => Path.GetFullPath(GetString(range))).ToList());

		FilesNamesMakeAbsoluteRelativeDialog.Result Command_Files_Name_MakeAbsolute_Dialog() => FilesNamesMakeAbsoluteRelativeDialog.Run(WindowParent, GetVariables(), true, true);

		void Command_Files_Name_MakeAbsolute(FilesNamesMakeAbsoluteRelativeDialog.Result result)
		{
			var results = GetFixedExpressionResults<string>(result.Expression);
			ReplaceSelections(GetSelectionStrings().Select((str, index) => new Uri(new Uri(results[index] + (result.Type == FilesNamesMakeAbsoluteRelativeDialog.ResultType.Directory ? "\\" : "")), str).LocalPath).ToList());
		}

		FilesNamesMakeAbsoluteRelativeDialog.Result Command_Files_Name_MakeRelative_Dialog() => FilesNamesMakeAbsoluteRelativeDialog.Run(WindowParent, GetVariables(), false, true);

		void Command_Files_Name_MakeRelative(FilesNamesMakeAbsoluteRelativeDialog.Result result)
		{
			var results = GetFixedExpressionResults<string>(result.Expression);
			if (result.Type == FilesNamesMakeAbsoluteRelativeDialog.ResultType.File)
				results = results.Select(str => Path.GetDirectoryName(str)).ToList();
			ReplaceSelections(GetSelectionStrings().Select((str, index) => GetRelativePath(str, results[index])).ToList());
		}

		FilesNamesGetUniqueDialog.Result Command_Files_Name_GetUnique_Dialog() => FilesNamesGetUniqueDialog.Run(WindowParent);

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
					if ((result.CheckExisting) && (FileOrDirectoryExists(newFileName)))
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
				Message.Show($"The following error(s) occurred:\n{string.Join("\n", errors)}", "Error", WindowParent);
		}

		void Command_Files_Get_VersionControlStatus() => ReplaceSelections(new VCS().GetStatus(RelativeSelectedFiles()).Select(x => x.ToString()).ToList());

		FilesSetSizeDialog.Result Command_Files_Set_Size_Dialog()
		{
			var vars = GetVariables();
			var sizes = RelativeSelectedFiles().AsParallel().AsOrdered().Select(file => new FileInfo(file).Length);
			vars.Add(NEVariable.List("size", "File size", () => sizes));
			return FilesSetSizeDialog.Run(WindowParent, vars);
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

		FilesSetTimeDialog.Result Command_Files_Set_Time_Dialog() => FilesSetTimeDialog.Run(WindowParent, GetVariables(), $@"""{DateTime.Now}""");

		void Command_Files_Set_Time(TimestampType type, FilesSetTimeDialog.Result result)
		{
			var dateTimes = GetFixedExpressionResults<DateTime>(result.Expression);
			var files = RelativeSelectedFiles();
			for (var ctr = 0; ctr < files.Count; ++ctr)
			{
				var dateTime = dateTimes[ctr];
				var file = files[ctr];
				if (!FileOrDirectoryExists(file))
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

			return FilesSetAttributesDialog.Run(WindowParent, current);
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

		FindBinaryDialog.Result Command_Files_Find_Binary_Dialog() => FindBinaryDialog.Run(WindowParent);

		void Command_Files_Find_Binary(FindBinaryDialog.Result result, ref Message.OptionsEnum answer)
		{
			var sels = new List<Range>();
			var selected = RelativeSelectedFiles().Zip(Selections, (fileName, range) => new { fileName, range }).ToList();
			var searcher = Helpers.GetSearcher(new List<string> { result.Text }, result.CodePages, result.MatchCase);
			foreach (var obj in selected)
				if (BinarySearchFile(obj.fileName, searcher, ref answer))
					sels.Add(obj.range);
			SetSelections(sels);
		}

		FindTextDialog.Result Command_Files_Find_Text_Dialog() => FindTextDialog.Run(WindowParent);

		void Command_Files_Find_Text(FindTextDialog.Result result, ref Message.OptionsEnum answer)
		{
			var sels = new List<Range>();
			var selected = RelativeSelectedFiles().Zip(Selections, (fileName, range) => new { fileName, range }).ToList();
			foreach (var obj in selected)
				if (TextSearchFile(obj.fileName, result, ref answer))
					sels.Add(obj.range);
			SetSelections(sels);
		}

		FilesFindMassFindDialog.Result Command_Files_Find_MassFind_Dialog() => FilesFindMassFindDialog.Run(WindowParent, GetVariables());

		void Command_Files_Find_MassFind(FilesFindMassFindDialog.Result result, ref Message.OptionsEnum answer)
		{
			var findStrs = GetVariableExpressionResults<string>(result.Expression);
			var searcher = Helpers.GetSearcher(findStrs, result.CodePages, result.MatchCase);
			var sels = new List<Range>();
			var selected = RelativeSelectedFiles().Zip(Selections, (fileName, range) => new { fileName, range }).ToList();
			foreach (var obj in selected)
				if (BinarySearchFile(obj.fileName, searcher, ref answer))
					sels.Add(obj.range);
			SetSelections(sels);
		}

		FilesInsertDialog.Result Command_Files_Insert_Dialog() => FilesInsertDialog.Run(WindowParent);

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

		FilesCreateFromExpressionsDialog.Result Command_Files_Create_FromExpressions_Dialog() => FilesCreateFromExpressionsDialog.Run(WindowParent, GetVariables(), CodePage);

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

		void Command_Files_Select_Files() => SetSelections(Selections.Where(range => File.Exists(FileName.RelativeChild(GetString(range)))).ToList());

		void Command_Files_Select_Directories() => SetSelections(Selections.Where(range => Directory.Exists(FileName.RelativeChild(GetString(range)))).ToList());

		void Command_Files_Select_Existing(bool existing) => SetSelections(Selections.Where(range => FileOrDirectoryExists(FileName.RelativeChild(GetString(range))) == existing).ToList());

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

		FilesSelectByVersionControlStatusDialog.Result Command_Files_Select_ByVersionControlStatus_Dialog() => FilesSelectByVersionControlStatusDialog.Run(WindowParent);

		void Command_Files_Select_ByVersionControlStatus(FilesSelectByVersionControlStatusDialog.Result result)
		{
			var statuses = new VCS().GetStatus(RelativeSelectedFiles()).ToList();
			var sels = Selections.Zip(statuses, (range, status) => new { range, status }).Where(obj => result.Statuses.Contains(obj.status)).Select(obj => obj.range).ToList();
			SetSelections(sels);
		}

		HashDialog.Result Command_Files_Hash_Dialog() => HashDialog.Run(WindowParent);

		void Command_Files_Hash(HashDialog.Result result) => ReplaceSelections(MultiProgressDialog.RunAsync(WindowParent, "Calculating hashes...", RelativeSelectedFiles(), (file, progress, cancel) => Hasher.GetAsync(file, result.HashType, result.HMACKey, progress, cancel)));

		FilesSignDialog.Result Command_Files_Sign_Dialog() => FilesSignDialog.Run(WindowParent);

		void Command_Files_Sign(FilesSignDialog.Result result) => ReplaceSelections(RelativeSelectedFiles().Select(file => Cryptor.Sign(file, result.CryptorType, result.Key, result.Hash)).ToList());

		FilesOperationsCopyMoveDialog.Result Command_Files_Operations_CopyMove_Dialog(bool move) => FilesOperationsCopyMoveDialog.Run(WindowParent, GetVariables(), move);

		void Command_Files_Operations_CopyMove(FilesOperationsCopyMoveDialog.Result result, bool move)
		{
			var variables = GetVariables();

			var oldFileNameExpression = new NEExpression(result.OldFileName);
			var newFileNameExpression = new NEExpression(result.NewFileName);
			var resultCount = variables.ResultCount(oldFileNameExpression, newFileNameExpression);

			var oldFileNames = oldFileNameExpression.EvaluateList<string>(variables, resultCount);
			var newFileNames = newFileNameExpression.EvaluateList<string>(variables, resultCount);

			const int InvalidCount = 10;
			var invalid = oldFileNames.Distinct().Where(name => !FileOrDirectoryExists(name)).Take(InvalidCount).ToList();
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

			if (new Message(WindowParent)
			{
				Title = "Confirm",
				Text = $"Are you sure you want to {(move ? "move" : "copy")} these {resultCount} files/directories?",
				Options = Message.OptionsEnum.YesNo,
				DefaultAccept = Message.OptionsEnum.Yes,
				DefaultCancel = Message.OptionsEnum.No,
			}.Show() != Message.OptionsEnum.Yes)
				return;

			invalid = newFileNames.Where(pair => File.Exists(pair)).Distinct().Take(InvalidCount).ToList();
			if (invalid.Any())
			{
				if (new Message(WindowParent)
				{
					Title = "Confirm",
					Text = $"Are you sure you want to overwrite these files:\n{string.Join("\n", invalid)}",
					Options = Message.OptionsEnum.YesNo,
					DefaultAccept = Message.OptionsEnum.Yes,
					DefaultCancel = Message.OptionsEnum.No,
				}.Show() != Message.OptionsEnum.Yes)
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
					if (File.Exists(newFileNames[ctr]))
						File.Delete(newFileNames[ctr]);

					if (move)
						File.Move(oldFileNames[ctr], newFileNames[ctr]);
					else
						File.Copy(oldFileNames[ctr], newFileNames[ctr]);
				}
		}

		void Command_Files_Operations_Delete()
		{
			if (new Message(WindowParent)
			{
				Title = "Confirm",
				Text = "Are you sure you want to delete these files/directories?",
				Options = Message.OptionsEnum.YesNo,
				DefaultCancel = Message.OptionsEnum.No,
			}.Show() != Message.OptionsEnum.Yes)
				return;

			var files = RelativeSelectedFiles();
			var answer = Message.OptionsEnum.None;
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
					if (answer != Message.OptionsEnum.YesToAll)
						answer = new Message(WindowParent)
						{
							Title = "Confirm",
							Text = $"An error occurred:\n\n{ex.Message}\n\nContinue?",
							Options = Message.OptionsEnum.YesNoYesAll,
							DefaultAccept = Message.OptionsEnum.Yes,
							DefaultCancel = Message.OptionsEnum.No,
						}.Show();

					if (answer == Message.OptionsEnum.No)
						break;
				}
			}
		}

		void Command_Files_Operations_DragDrop()
		{
			var strs = RelativeSelectedFiles();
			if (!StringsAreFiles(strs))
				throw new Exception("Selections must be files.");
			doDrag = DragType.Selections;
		}

		void Command_Files_Operations_OpenDisk() => Launcher.Static.LaunchDisk(files: RelativeSelectedFiles());

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

		void Command_Files_Operations_RunCommand_Parallel() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => RunCommand(GetString(range))).ToList());

		void Command_Files_Operations_RunCommand_Sequential() => ReplaceSelections(GetSelectionStrings().Select(str => RunCommand(str)).ToList());

		void Command_Files_Operations_RunCommand_Shell() => GetSelectionStrings().ForEach(str => Process.Start(str));

		FilesOperationsEncodingDialog.Result Command_Files_Operations_Encoding_Dialog() => FilesOperationsEncodingDialog.Run(WindowParent);

		void Command_Files_Operations_Encoding(FilesOperationsEncodingDialog.Result result) => MultiProgressDialog.Run(WindowParent, "Changing encoding...", RelativeSelectedFiles(), (inputFile, progress, cancel) => ReencodeFile(inputFile, progress, cancel, result.InputCodePage, result.OutputCodePage));
	}
}
