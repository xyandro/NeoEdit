﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Models;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.TaskRunning;

namespace NeoEdit.Editor
{
	partial class NEFileHandler
	{
		static void AddFilesFromClipboards(NEFilesHandler neFiles)
		{
			var index = 0;
			foreach (var strs in NEClipboard.Current)
			{
				++index;
				var ending = strs.Any(str => (!str.EndsWith("\r")) && (!str.EndsWith("\n"))) ? "\r\n" : "";
				var sb = new StringBuilder(strs.Sum(str => str.Length + ending.Length));
				var sels = new List<Range>();
				foreach (var str in strs)
				{
					var start = sb.Length;
					sb.Append(str);
					sels.Add(new Range(sb.Length, start));
					sb.Append(ending);
				}
				var te = new NEFileHandler(displayName: $"Clipboard {index}", bytes: Coder.StringToBytes(sb.ToString(), Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, modified: false);
				neFiles.AddNewFile(te);
				te.Selections = sels;
			}
		}

		static void AddFilesFromClipboardSelections(NEFilesHandler neFiles) => NEClipboard.Current.Strings.ForEach((str, index) => neFiles.AddNewFile(new NEFileHandler(displayName: $"Clipboard {index + 1}", bytes: Coder.StringToBytes(str, Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, modified: false)));

		string GetSaveFileName()
		{
			return NEFiles.ShowFile(this, () =>
			{
				var result = NEFiles.FilesWindow.RunSaveFileDialog(Path.GetFileName(FileName) ?? DisplayName, "txt", Path.GetDirectoryName(FileName), "All files|*.*");
				if (result == null)
					throw new OperationCanceledException();

				if (Directory.Exists(result.FileName))
					throw new Exception("A directory by that name already exists");
				if (!Directory.Exists(Path.GetDirectoryName(result.FileName)))
					throw new Exception("Directory doesn't exist");
				return result.FileName;
			});
		}

		void InsertFiles(IEnumerable<string> fileNames)
		{
			if ((Selections.Count != 1) && (Selections.Count != fileNames.Count()))
				throw new Exception("Must have either one or equal number of selections.");

			var strs = new List<string>();
			foreach (var fileName in fileNames)
			{
				var bytes = File.ReadAllBytes(fileName);
				strs.Add(Coder.BytesToString(bytes, Coder.CodePage.AutoByBOM, true));
			}

			if (Selections.Count == 1)
				ReplaceOneWithMany(strs, false);
			if (Selections.Count == fileNames.Count())
				ReplaceSelections(strs);
		}

		static bool PreExecute_File_Select_All()
		{
			EditorExecuteState.CurrentState.NEFiles.ActiveFiles = EditorExecuteState.CurrentState.NEFiles.AllFiles;
			EditorExecuteState.CurrentState.NEFiles.Focused = EditorExecuteState.CurrentState.NEFiles.ActiveFiles.FirstOrDefault();
			return true;
		}

		static bool PreExecute_File_Select_None()
		{
			EditorExecuteState.CurrentState.NEFiles.ClearActiveFiles();
			return true;
		}

		static bool PreExecute_File_Select_WithWithoutSelections(bool hasSelections)
		{
			EditorExecuteState.CurrentState.NEFiles.SetActiveFiles(EditorExecuteState.CurrentState.NEFiles.ActiveFiles.Where(neFile => neFile.Selections.Any() == hasSelections));
			return true;
		}

		static bool PreExecute_File_Select_ModifiedUnmodified(bool modified)
		{
			EditorExecuteState.CurrentState.NEFiles.SetActiveFiles(EditorExecuteState.CurrentState.NEFiles.ActiveFiles.Where(neFile => neFile.IsModified == modified));
			return true;
		}

		static bool PreExecute_File_Select_Inactive()
		{
			EditorExecuteState.CurrentState.NEFiles.SetActiveFiles(EditorExecuteState.CurrentState.NEFiles.AllFiles.Except(EditorExecuteState.CurrentState.NEFiles.ActiveFiles));
			return true;
		}

		static bool PreExecute_File_Select_Choose()
		{
			var data = new WindowActiveFilesDialogData();
			void RecalculateData()
			{
				data.AllFiles = EditorExecuteState.CurrentState.NEFiles.AllFiles.Select(neFile => neFile.NEFileLabel).ToList();
				data.ActiveIndexes = EditorExecuteState.CurrentState.NEFiles.ActiveFiles.Select(neFile => EditorExecuteState.CurrentState.NEFiles.AllFiles.IndexOf(neFile)).ToList();
				data.FocusedIndex = EditorExecuteState.CurrentState.NEFiles.AllFiles.IndexOf(EditorExecuteState.CurrentState.NEFiles.Focused);
			}
			RecalculateData();
			data.SetActiveIndexes = list =>
			{
				//EditorExecuteState.CurrentState.NEFiles.ClearAllActive();
				//list.Select(index => EditorExecuteState.CurrentState.NEFiles.AllFiles[index]).ForEach(neFile => EditorExecuteState.CurrentState.NEFiles.SetActive(neFile));
				//RecalculateData();
				//EditorExecuteState.CurrentState.NEFiles.RenderFilesWindow();
			};
			data.CloseFiles = list =>
			{
				//var neFiles = list.Select(index => EditorExecuteState.CurrentState.NEFiles.AllFiles[index]).ToList();
				//neFiles.ForEach(neFile => neFile.VerifyCanClose());
				//neFiles.ForEach(neFile => EditorExecuteState.CurrentState.NEFiles.RemoveFile(neFile));
				//RecalculateData();
				//EditorExecuteState.CurrentState.NEFiles.RenderFilesWindow();
			};
			data.DoMoves = moves =>
			{
				//moves.ForEach(((int oldIndex, int newIndex) move) => EditorExecuteState.CurrentState.NEFiles.MoveFile(EditorExecuteState.CurrentState.NEFiles.AllFiles[move.oldIndex], move.newIndex));
				//RecalculateData();
				//EditorExecuteState.CurrentState.NEFiles.RenderFilesWindow();
			};

			EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_PreExecute_File_Select_Choose(data);

			return true;
		}

		static bool PreExecute_File_New_New()
		{
			EditorExecuteState.CurrentState.NEFiles.AddNewFile(new NEFileHandler());
			return true;
		}

		static bool PreExecute_File_New_FromClipboard_Selections()
		{
			AddFilesFromClipboardSelections(EditorExecuteState.CurrentState.NEFiles);
			return true;
		}

		static bool PreExecute_File_New_FromClipboard_Files()
		{
			AddFilesFromClipboards(EditorExecuteState.CurrentState.NEFiles);
			return true;
		}

		static bool PreExecute_File_New_WordList()
		{
			byte[] data;
			var streamName = typeof(NEFilesHandler).Assembly.GetManifestResourceNames().Where(name => name.EndsWith(".Words.txt.gz")).Single();
			using (var stream = typeof(NEFilesHandler).Assembly.GetManifestResourceStream(streamName))
			using (var ms = new MemoryStream())
			{
				stream.CopyTo(ms);
				data = ms.ToArray();
			}

			data = Compressor.Decompress(data, Compressor.Type.GZip);
			data = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(data));
			EditorExecuteState.CurrentState.NEFiles.AddNewFile(new NEFileHandler(displayName: "Word List", bytes: data, modified: false));

			return true;
		}

		static void Configure_FileMacro_Open_Open(string initialDirectory = null)
		{
			if ((initialDirectory == null) && (EditorExecuteState.CurrentState.NEFiles.Focused != null))
				initialDirectory = Path.GetDirectoryName(EditorExecuteState.CurrentState.NEFiles.Focused.FileName);
			var result = EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_FileMacro_Open_Open("txt", initialDirectory, "Text files|*.txt|All files|*.*", 2, true);
			if (result == null)
				throw new OperationCanceledException();
			EditorExecuteState.CurrentState.Configuration = result;
		}

		static bool PreExecute_FileMacro_Open_Open()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_FileMacro_Open_Open;
			result.FileNames.ForEach(fileName => EditorExecuteState.CurrentState.NEFiles.AddNewFile(new NEFileHandler(fileName)));
			return true;
		}

		static bool PreExecute_File_Open_CopiedCut()
		{
			NEClipboard.Current.Strings.AsTaskRunner().Select(file => new NEFileHandler(file)).ForEach(neFile => EditorExecuteState.CurrentState.NEFiles.AddNewFile(neFile));
			return true;
		}

		static void Configure_File_Open_ReopenWithEncoding() => EditorExecuteState.CurrentState.Configuration = EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_File_OpenEncoding_ReopenWithEncoding(EditorExecuteState.CurrentState.NEFiles.Focused.CodePage);

		void Execute_File_Open_ReopenWithEncoding()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_File_OpenEncoding_ReopenWithEncoding;
			if (IsModified)
			{
				if (!QueryUser(nameof(Execute_File_Open_ReopenWithEncoding), "You have unsaved changes. Are you sure you want to reload?", MessageOptions.Yes))
					return;
			}

			OpenFile(FileName, codePage: result.CodePage);
		}

		void Execute_File_Refresh()
		{
			if (string.IsNullOrEmpty(FileName))
				return;

			if (!File.Exists(FileName))
				throw new Exception("This file has been deleted.");

			if (fileLastWrite != new FileInfo(FileName).LastWriteTime)
			{
				if (!QueryUser(nameof(Execute_File_Refresh), "This file has been updated on disk. Reload?", MessageOptions.Yes))
					return;

				Execute_File_Revert();
			}
		}

		void Execute_File_AutoRefresh() => SetAutoRefresh(EditorExecuteState.CurrentState.MultiStatus != true);

		void Execute_File_Revert()
		{
			if (IsModified)
			{
				if (!QueryUser(nameof(Execute_File_Revert), "You have unsaved changes. Are you sure you want to reload?", MessageOptions.No))
					return;
			}

			var selections = Selections.ToList();
			var regions = Enumerable.Range(1, 9).ToDictionary(index => index, index => GetRegions(index));

			OpenFile(FileName, DisplayName, keepUndo: true);

			Func<IReadOnlyList<Range>, IReadOnlyList<Range>> reformatRanges = l => l.Select(range => new Range(Math.Max(0, Math.Min(range.Cursor, Text.Length)), Math.Max(0, Math.Min(range.Anchor, Text.Length)))).ToList();
			Selections = reformatRanges(selections);
			for (var region = 1; region <= 9; ++region)
				SetRegions(region, reformatRanges(GetRegions(region)));
		}

		void Execute_File_Save_SaveModified()
		{
			if ((FileName != null) && (!IsModified))
				return;
			Execute_File_Save_SaveAll();
		}

		void Execute_File_Save_SaveAll()
		{
			if (FileName == null)
				Execute_File_SaveCopy_SaveAsCopy();
			else
				Save(FileName);
		}

		void Execute_File_SaveCopy_SaveAsCopy(bool copyOnly = false) => Save(GetSaveFileName(), copyOnly);

		static void Configure_File_SaveCopyAdvanced_SaveAsCopyByExpressionSetDisplayName() => EditorExecuteState.CurrentState.Configuration = EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_FileTable_Various_Various(EditorExecuteState.CurrentState.NEFiles.Focused.GetVariables(), EditorExecuteState.CurrentState.NEFiles.Focused.Selections.Count);

		void Execute_File_SaveCopy_SaveAsCopyByExpression(bool copyOnly = false)
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_FileTable_Various_Various;
			var results = GetExpressionResults<string>(result.Expression, Selections.Count());
			if (results.Count != 1)
				throw new Exception("Only one filename may be specified");

			var newFileName = FileName.RelativeChild(results[0]);

			if (File.Exists(newFileName))
			{
				if (!QueryUser(nameof(Execute_File_SaveCopy_SaveAsCopyByExpression), "File already exists; overwrite?", MessageOptions.None))
					return;
			}

			Save(newFileName, copyOnly);
		}

		void Execute_File_Move_Move()
		{
			if (string.IsNullOrEmpty(FileName))
			{
				Execute_File_SaveCopy_SaveAsCopy();
				return;
			}

			var fileName = GetSaveFileName();

			if (!string.Equals(FileName, fileName, StringComparison.OrdinalIgnoreCase))
				File.Delete(fileName);
			File.Move(FileName, fileName);
			SetFileName(fileName);
		}

		static void Configure_File_Move_MoveByExpression() => EditorExecuteState.CurrentState.Configuration = EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_FileTable_Various_Various(EditorExecuteState.CurrentState.NEFiles.Focused.GetVariables(), EditorExecuteState.CurrentState.NEFiles.Focused.Selections.Count);

		void Execute_File_Move_MoveByExpression()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_FileTable_Various_Various;
			var results = GetExpressionResults<string>(result.Expression, Selections.Count());
			if (results.Count != 1)
				throw new Exception("Only one filename may be specified");

			var newFileName = FileName.RelativeChild(results[0]);

			if ((!string.Equals(newFileName, FileName, StringComparison.OrdinalIgnoreCase)) && (File.Exists(newFileName)))
			{
				if (!QueryUser(nameof(Execute_File_Move_MoveByExpression), "File already exists; overwrite?", MessageOptions.None))
					return;
			}

			if (FileName != null)
			{
				if (!string.Equals(FileName, newFileName, StringComparison.OrdinalIgnoreCase))
					File.Delete(newFileName);
				File.Move(FileName, newFileName);
			}
			SetFileName(newFileName);
		}

		void Execute_File_Copy_Path() => ClipboardCopy = new List<string> { FileName };

		void Execute_File_Copy_Name() => Clipboard = new List<string> { Path.GetFileName(FileName) };

		void Execute_File_Copy_DisplayName() => Clipboard = new List<string> { DisplayName ?? Path.GetFileName(FileName) };

		void Execute_File_Delete()
		{
			if (FileName == null)
				return;

			if (!QueryUser(nameof(Execute_File_Delete), "Are you sure you want to delete this file?", MessageOptions.No))
				return;

			File.Delete(FileName);
		}

		static void Configure_File_Encoding() => EditorExecuteState.CurrentState.Configuration = EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_File_OpenEncoding_ReopenWithEncoding(EditorExecuteState.CurrentState.NEFiles.Focused.CodePage);

		void Execute_File_Encoding()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_File_OpenEncoding_ReopenWithEncoding;
			CodePage = result.CodePage;
		}

		static void Configure_File_LineEndings()
		{
			var endings = EditorExecuteState.CurrentState.NEFiles.ActiveFiles.Select(neFile => neFile.Text.OnlyEnding).Distinct().Take(2).ToList();
			var ending = endings.Count == 1 ? endings[0] : "";
			EditorExecuteState.CurrentState.Configuration = EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_File_LineEndings(ending);
		}

		void Execute_File_LineEndings()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_File_LineEndings;
			var lines = Text.NumLines;
			var sel = new List<Range>();
			for (var line = 0; line < lines; ++line)
			{
				var current = Text.GetEnding(line);
				if ((current.Length == 0) || (current == result.LineEndings))
					continue;
				var start = Text.GetPosition(line, Text.GetLineLength(line));
				sel.Add(Range.FromIndex(start, current.Length));
			}
			Replace(sel, sel.Select(str => result.LineEndings).ToList());
		}

		void Execute_File_FileActiveFileIndex(bool activeOnly)
		{
			ReplaceSelections((NEFiles.GetFileIndex(this, activeOnly) + 1).ToString());
		}

		void Execute_File_Advanced_Compress() => Compressed = EditorExecuteState.CurrentState.MultiStatus == false;

		static void Configure_File_Advanced_Encrypt()
		{
			if (EditorExecuteState.CurrentState.MultiStatus != false)
				EditorExecuteState.CurrentState.Configuration = new Configuration_File_Advanced_Encrypt();
			else
				EditorExecuteState.CurrentState.Configuration = EditorExecuteState.CurrentState.NEFiles.FilesWindow.RunDialog_Configure_File_Advanced_Encrypt(Cryptor.Type.AES, true);
		}

		void Execute_File_Advanced_Encrypt() => AESKey = (EditorExecuteState.CurrentState.Configuration as Configuration_File_Advanced_Encrypt).Key;

		void Execute_File_Advanced_Explore() => Process.Start("explorer.exe", $"/select,\"{FileName}\"");

		void Execute_File_Advanced_CommandPrompt() => Process.Start(new ProcessStartInfo("cmd.exe") { WorkingDirectory = Path.GetDirectoryName(FileName) });

		void Execute_File_Advanced_DragDrop() => AddDragFile(FileName);

		void Execute_File_Advanced_SetDisplayName()
		{
			var result = EditorExecuteState.CurrentState.Configuration as Configuration_FileTable_Various_Various;
			if (result.Expression == "f")
			{
				DisplayName = null;
				return;
			}
			var results = GetExpressionResults<string>(result.Expression);
			if (results.Count != 1)
				throw new Exception("Only one value may be specified");
			DisplayName = results[0];
		}

		static bool PreExecute_File_Advanced_DontExitOnClose()
		{
			Settings.DontExitOnClose = EditorExecuteState.CurrentState.MultiStatus != true;
			return true;
		}

		static bool PreExecute_File_Close_ActiveInactiveFiles(bool active)
		{
			foreach (var neFile in (active ? EditorExecuteState.CurrentState.NEFiles.ActiveFiles : EditorExecuteState.CurrentState.NEFiles.AllFiles.Except(EditorExecuteState.CurrentState.NEFiles.ActiveFiles)))
			{
				neFile.VerifyCanClose();
				neFile.ClearFiles();
			}

			return true;
		}

		void Execute_File_Close_FilesWithWithoutSelections(bool hasSelections)
		{
			if (Selections.Any() == hasSelections)
			{
				VerifyCanClose();
				ClearFiles();
			}
		}

		void Execute_File_Close_ModifiedUnmodifiedFiles(bool modified)
		{
			if (IsModified == modified)
			{
				VerifyCanClose();
				ClearFiles();
			}
		}

		static bool PreExecute_File_Exit()
		{
			foreach (var neFile in EditorExecuteState.CurrentState.NEFiles.AllFiles)
			{
				EditorExecuteState.CurrentState.NEFiles.AddToTransaction(neFile);
				neFile.VerifyCanClose();
				neFile.ClearFiles();
			}
			NEFilesHandler.AllNEFiles.Remove(EditorExecuteState.CurrentState.NEFiles);
			EditorExecuteState.CurrentState.NEFiles.FilesWindow.CloseWindow();

			if (!NEFilesHandler.AllNEFiles.Any())
			{
				if (((EditorExecuteState.CurrentState.Configuration as Configuration_File_Exit)?.WindowClosed != true) || (!Settings.DontExitOnClose))
					Environment.Exit(0);

				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();

				// Restart if memory usage is more than 1/2 GB
				var process = Process.GetCurrentProcess();
				if (process.PrivateMemorySize64 > (1 << 29))
				{
					Process.Start(Environment.GetCommandLineArgs()[0], $"-background -waitpid={process.Id}");
					Environment.Exit(0);
				}
			}

			return true;
		}
	}
}