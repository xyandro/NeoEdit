using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

		List<string> GetDirectoryContents(string dir, bool recursive)
		{
			var dirs = new List<string> { dir };
			var results = new List<string>();
			for (var ctr = 0; ctr < dirs.Count; ++ctr)
			{
				var subDirs = Directory.GetDirectories(dirs[ctr]);
				dirs.AddRange(subDirs);
				results.AddRange(subDirs);
				results.AddRange(Directory.GetFiles(dirs[ctr]));
				if (!recursive)
					break;
			}
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

		void Command_Files_Names_Simplify() => ReplaceSelections(Selections.Select(range => Path.GetFullPath(GetString(range))).ToList());

		MakeAbsoluteDialog.Result Command_Files_Names_MakeAbsolute_Dialog() => MakeAbsoluteDialog.Run(WindowParent, GetVariables(), true);

		void Command_Files_Names_MakeAbsolute(MakeAbsoluteDialog.Result result)
		{
			var results = GetFixedExpressionResults<string>(result.Expression);
			ReplaceSelections(GetSelectionStrings().Select((str, index) => new Uri(new Uri(results[index] + (result.Type == MakeAbsoluteDialog.ResultType.Directory ? "\\" : "")), str).LocalPath).ToList());
		}

		GetUniqueNamesDialog.Result Command_Files_Names_GetUnique_Dialog() => GetUniqueNamesDialog.Run(WindowParent);

		void Command_Files_Names_GetUnique(GetUniqueNamesDialog.Result result)
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

		void Command_Files_Names_Sanitize() => ReplaceSelections(Selections.Select(range => SanitizeFileName(GetString(range))).ToList());

		void Command_Files_Get_Size() => ReplaceSelections(RelativeSelectedFiles().Select(file => GetSize(file)).ToList());

		void Command_Files_Get_WriteTime()
		{
			var files = RelativeSelectedFiles();
			var strs = new List<string>();
			foreach (var file in files)
			{
				if (File.Exists(file))
				{
					var fileinfo = new FileInfo(file);
					strs.Add(fileinfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
				}
				else if (Directory.Exists(file))
				{
					var dirinfo = new DirectoryInfo(file);
					strs.Add(dirinfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
				}
				else
					strs.Add("INVALID");
			}
			ReplaceSelections(strs);
		}

		void Command_Files_Get_AccessTime()
		{
			var files = RelativeSelectedFiles();
			var strs = new List<string>();
			foreach (var file in files)
			{
				if (File.Exists(file))
				{
					var fileinfo = new FileInfo(file);
					strs.Add(fileinfo.LastAccessTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
				}
				else if (Directory.Exists(file))
				{
					var dirinfo = new DirectoryInfo(file);
					strs.Add(dirinfo.LastAccessTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
				}
				else
					strs.Add("INVALID");
			}
			ReplaceSelections(strs);
		}

		void Command_Files_Get_CreateTime()
		{
			var files = RelativeSelectedFiles();
			var strs = new List<string>();
			foreach (var file in files)
			{
				if (File.Exists(file))
				{
					var fileinfo = new FileInfo(file);
					strs.Add(fileinfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
				}
				else if (Directory.Exists(file))
				{
					var dirinfo = new DirectoryInfo(file);
					strs.Add(dirinfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
				}
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

		SetSizeDialog.Result Command_Files_Set_Size_Dialog()
		{
			var vars = GetVariables();
			var sizes = RelativeSelectedFiles().AsParallel().AsOrdered().Select(file => new FileInfo(file).Length);
			vars.Add(NEVariable.List("size", "File size", () => sizes));
			return SetSizeDialog.Run(WindowParent, vars);
		}

		void Command_Files_Set_Size(SetSizeDialog.Result result)
		{
			var vars = GetVariables();
			var files = RelativeSelectedFiles();
			var sizes = files.AsParallel().AsOrdered().Select(file => new FileInfo(file).Length);
			vars.Add(NEVariable.List("size", "File size", () => sizes));
			var results = new NEExpression(result.Expression).EvaluateRows<long>(vars, Selections.Count()).Select(size => size * result.Factor).ToList();
			files.Zip(results, (file, size) => new { file, size }).AsParallel().ForEach(obj => SetFileSize(obj.file, obj.size));
		}

		ChooseDateTimeDialog.Result Command_Files_Set_Time_Dialog() => ChooseDateTimeDialog.Run(WindowParent, DateTime.Now);

		void Command_Files_Set_Time(TimestampType type, ChooseDateTimeDialog.Result result)
		{
			var files = RelativeSelectedFiles();
			foreach (var file in files)
			{
				if (!FileOrDirectoryExists(file))
					File.WriteAllBytes(file, new byte[0]);

				if (File.Exists(file))
				{
					var info = new FileInfo(file);
					if (type.HasFlag(TimestampType.Write))
						info.LastWriteTime = result.Value;
					if (type.HasFlag(TimestampType.Access))
						info.LastAccessTime = result.Value;
					if (type.HasFlag(TimestampType.Create))
						info.CreationTime = result.Value;
				}
				else if (Directory.Exists(file))
				{
					var info = new DirectoryInfo(file);
					if (type.HasFlag(TimestampType.Write))
						info.LastWriteTime = result.Value;
					if (type.HasFlag(TimestampType.Access))
						info.LastAccessTime = result.Value;
					if (type.HasFlag(TimestampType.Create))
						info.CreationTime = result.Value;
				}
			}
		}

		SetAttributesDialog.Result Command_Files_Set_Attributes_Dialog()
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

			return SetAttributesDialog.Run(WindowParent, current);
		}

		void Command_Files_Set_Attributes(SetAttributesDialog.Result result)
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

		void Command_Files_Directory_GetChildrenDescendants(bool recursive)
		{
			var dirs = RelativeSelectedFiles();
			if (dirs.Any(dir => !Directory.Exists(dir)))
				throw new ArgumentException("Path must be of existing directories");

			ReplaceSelections(dirs.Select(dir => string.Join(Data.DefaultEnding, GetDirectoryContents(dir, recursive))).ToList());
		}

		InsertFilesDialog.Result Command_Files_Insert_Dialog() => InsertFilesDialog.Run(WindowParent);

		void Command_Files_Insert(InsertFilesDialog.Result result) => ReplaceSelections(GetSelectionStrings().AsParallel().AsOrdered().Select(fileName => Coder.BytesToString(File.ReadAllBytes(fileName), result.CodePage, true)).ToList());

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

		CreateFilesDialog.Result Command_Files_Create_FromExpressions_Dialog() => CreateFilesDialog.Run(WindowParent, GetVariables(), CodePage);

		void Command_Files_Create_FromExpressions(CreateFilesDialog.Result result)
		{
			var variables = GetVariables();

			var datas = new NEExpression(result.Data).EvaluateRows<string>(variables);
			if (!datas.Any())
				throw new Exception("No data");

			var fileNames = new NEExpression(result.FileName).EvaluateRows<string>(variables, datas.Count);
			if (!fileNames.Any())
				throw new Exception("No filenames");

			if (fileNames.Count != datas.Count)
				throw new Exception("File name and data count must match.");

			var outputs = fileNames.Zip(datas, (filename, data) => new { filename, data }).ToList();
			foreach (var output in outputs)
			{
				var bytes = Coder.StringToBytes(output.data, result.CodePage, true);
				File.WriteAllBytes(output.filename, bytes);
			}
		}

		void Command_Files_Select_Name(GetPathType type) => Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => GetPathRange(type, range)).ToList());

		void Command_Files_Select_Files() => Selections.Replace(Selections.Where(range => File.Exists(FileName.RelativeChild(GetString(range)))).ToList());

		void Command_Files_Select_Directories() => Selections.Replace(Selections.Where(range => Directory.Exists(FileName.RelativeChild(GetString(range)))).ToList());

		void Command_Files_Select_Existing(bool existing) => Selections.Replace(Selections.Where(range => FileOrDirectoryExists(FileName.RelativeChild(GetString(range))) == existing).ToList());

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

			Selections.Replace(sels.AsParallel().AsOrdered().Where(sel => roots.Contains(sel.str) == include).Select(sel => sel.range).ToList());
		}

		HashDialog.Result Command_Files_Hash_Dialog() => HashDialog.Run(WindowParent);

		void Command_Files_Hash(HashDialog.Result result) => ReplaceSelections(RelativeSelectedFiles().Select(file => Hasher.Get(file, result.HashType)).ToList());

		void Command_Files_Operations_CopyMove(bool move)
		{
			var strs = Selections.Select(range => GetString(range).Split(new string[] { "=>" }, StringSplitOptions.None).Select(str => FileName.RelativeChild(str.Trim())).ToList()).ToList();
			if (strs.Any(pair => (pair.Count != 2) || (pair.Any(item => string.IsNullOrEmpty(item)))))
				throw new Exception("Format: Source => Destination");

			var files = strs.Select(pair => new { source = pair[0], dest = pair[1] }).Where(pair => pair.source != pair.dest).ToList();
			if (files.Count == 0)
				throw new Exception("Nothing to do!");

			const int InvalidCount = 10;
			var invalid = files.Select(pair => pair.source).Distinct().Where(file => !FileOrDirectoryExists(file)).Take(InvalidCount).ToList();
			if (invalid.Any())
				throw new Exception($"Sources don't exist:\n{string.Join("\n", invalid)}");

			invalid = files.Select(pair => Path.GetDirectoryName(pair.dest)).Distinct().Where(dir => !Directory.Exists(dir)).Take(InvalidCount).ToList();
			if (invalid.Any())
				throw new Exception($"Directories don't exist:\n{string.Join("\n", invalid)}");

			// If user specified a file and a directory, assume they want the file (with the same name) in that directory
			files = files.Select(pair => new { source = pair.source, dest = (File.Exists(pair.source)) && (Directory.Exists(pair.dest)) ? Path.Combine(pair.dest, Path.GetFileName(pair.source)) : pair.dest }).ToList();

			invalid = files.Where(pair => (Directory.Exists(pair.dest)) || ((Directory.Exists(pair.source)) && (File.Exists(pair.dest)))).Select(pair => pair.dest).Distinct().Take(InvalidCount).ToList();
			if (invalid.Any())
				throw new Exception($"Destinations already exist:\n{string.Join("\n", invalid)}");

			if (new Message
			{
				Title = "Confirm",
				Text = $"Are you sure you want to {(move ? "move" : "copy")} these {files.Count} files/directories?",
				Options = Message.OptionsEnum.YesNo,
				DefaultAccept = Message.OptionsEnum.Yes,
				DefaultCancel = Message.OptionsEnum.No,
			}.Show() != Message.OptionsEnum.Yes)
				return;

			invalid = files.Select(pair => pair.dest).Where(pair => File.Exists(pair)).Distinct().Take(InvalidCount).ToList();
			if (invalid.Any())
			{
				if (new Message
				{
					Title = "Confirm",
					Text = $"Are you sure you want to overwrite these files:\n{string.Join("\n", invalid)}",
					Options = Message.OptionsEnum.YesNo,
					DefaultAccept = Message.OptionsEnum.Yes,
					DefaultCancel = Message.OptionsEnum.No,
				}.Show() != Message.OptionsEnum.Yes)
					return;
			}

			foreach (var pair in files)
				if (Directory.Exists(pair.source))
				{
					if (move)
						Directory.Move(pair.source, pair.dest);
					else
						CopyDirectory(pair.source, pair.dest);
				}
				else
				{
					if (File.Exists(pair.dest))
						File.Delete(pair.dest);

					if (move)
						File.Move(pair.source, pair.dest);
					else
						File.Copy(pair.source, pair.dest);
				}
		}

		void Command_Files_Operations_Delete()
		{
			if (new Message
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
						answer = new Message
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
	}
}
