using System;
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
using NeoEdit.Editor.PreExecution;
using NeoEdit.TaskRunning;

namespace NeoEdit.Editor
{
	partial class NEFileHandler
	{
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

		static PreExecutionStop PreExecute_File_Select_All(EditorExecuteState state)
		{
			state.NEFiles.AllFiles.ForEach(neFile => state.NEFiles.SetActive(neFile));
			state.NEFiles.Focused = state.NEFiles.AllFiles.FirstOrDefault();

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_File_Select_None(EditorExecuteState state)
		{
			state.NEFiles.ClearAllActive();
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_File_Select_WithWithoutSelections(EditorExecuteState state, bool hasSelections)
		{
			state.NEFiles.ActiveFiles.ForEach(neFile => state.NEFiles.SetActive(neFile, neFile.Selections.Any() == hasSelections));
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_File_Select_ModifiedUnmodified(EditorExecuteState state, bool modified)
		{
			state.NEFiles.ActiveFiles.ForEach(neFile => state.NEFiles.SetActive(neFile, neFile.IsModified == modified));
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_File_Select_Inactive(EditorExecuteState state)
		{
			state.NEFiles.AllFiles.ForEach(neFile => state.NEFiles.SetActive(neFile, !Enumerable.Contains<NEFileHandler>(state.NEFiles.ActiveFiles, neFile)));
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_File_Select_Choose(EditorExecuteState state)
		{
			var data = new WindowActiveFilesDialogData();
			void RecalculateData()
			{
				data.AllFiles = state.NEFiles.AllFiles.Select(neFile => neFile.NEFileLabel).ToList();
				data.ActiveIndexes = state.NEFiles.ActiveFiles.Select(neFile => state.NEFiles.AllFiles.IndexOf(neFile)).ToList();
				data.FocusedIndex = state.NEFiles.AllFiles.IndexOf(state.NEFiles.Focused);
			}
			RecalculateData();
			data.SetActiveIndexes = list =>
			{
				state.NEFiles.ClearAllActive();
				list.Select(index => state.NEFiles.AllFiles[index]).ForEach(neFile => state.NEFiles.SetActive(neFile));
				RecalculateData();
				state.NEFiles.RenderFilesWindow();
			};
			data.CloseFiles = list =>
			{
				var neFiles = list.Select(index => state.NEFiles.AllFiles[index]).ToList();
				neFiles.ForEach(neFile => neFile.VerifyCanClose());
				neFiles.ForEach(neFile => state.NEFiles.RemoveFile(neFile));
				RecalculateData();
				state.NEFiles.RenderFilesWindow();
			};
			data.DoMoves = moves =>
			{
				moves.ForEach(((int oldIndex, int newIndex) move) => state.NEFiles.MoveFile(state.NEFiles.AllFiles[move.oldIndex], move.newIndex));
				RecalculateData();
				state.NEFiles.RenderFilesWindow();
			};

			state.NEFiles.FilesWindow.RunDialog_PreExecute_File_Select_Choose(data);

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_File_New_New(EditorExecuteState state)
		{
			state.NEFiles.AddFile(new NEFileHandler(), canReplace: false);
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_File_New_FromClipboard_Selections(EditorExecuteState state)
		{
			NEFiles.AddFilesFromClipboardSelections(state.NEFiles);
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_File_New_FromClipboard_Files(EditorExecuteState state)
		{
			NEFiles.AddFilesFromClipboards(state.NEFiles);
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_File_New_WordList(EditorExecuteState state)
		{
			byte[] data;
			var streamName = typeof(NEFiles).Assembly.GetManifestResourceNames().Where(name => name.EndsWith(".Words.txt.gz")).Single();
			using (var stream = typeof(NEFiles).Assembly.GetManifestResourceStream(streamName))
			using (var ms = new MemoryStream())
			{
				stream.CopyTo(ms);
				data = ms.ToArray();
			}

			data = Compressor.Decompress(data, Compressor.Type.GZip);
			data = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(data));
			state.NEFiles.AddFile(new NEFileHandler(displayName: "Word List", bytes: data, modified: false));

			return PreExecutionStop.Stop;
		}

		static Configuration_FileMacro_Open_Open Configure_FileMacro_Open_Open(EditorExecuteState state, string initialDirectory = null)
		{
			if ((initialDirectory == null) && (state.NEFiles.Focused != null))
				initialDirectory = Path.GetDirectoryName(state.NEFiles.Focused.FileName);
			var result = state.NEFiles.FilesWindow.RunDialog_Configure_FileMacro_Open_Open("txt", initialDirectory, "Text files|*.txt|All files|*.*", 2, true);
			if (result == null)
				throw new OperationCanceledException();
			return result;
		}

		static PreExecutionStop PreExecute_FileMacro_Open_Open(EditorExecuteState state)
		{
			var result = state.Configuration as Configuration_FileMacro_Open_Open;
			result.FileNames.ForEach(fileName => state.NEFiles.AddFile(new NEFileHandler(fileName)));
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_File_Open_CopiedCut(EditorExecuteState state)
		{
			NEClipboard.Current.Strings.AsTaskRunner().Select(file => new NEFileHandler(file)).ForEach(neFile => state.NEFiles.AddFile(neFile));
			return PreExecutionStop.Stop;
		}

		static Configuration_File_OpenEncoding_ReopenWithEncoding Configure_File_Open_ReopenWithEncoding(EditorExecuteState state) => state.NEFiles.FilesWindow.RunDialog_Configure_File_OpenEncoding_ReopenWithEncoding(state.NEFiles.Focused.CodePage);

		void Execute_File_Open_ReopenWithEncoding()
		{
			var result = state.Configuration as Configuration_File_OpenEncoding_ReopenWithEncoding;
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

		void Execute_File_AutoRefresh() => SetAutoRefresh(state.MultiStatus != true);

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

		static Configuration_FileTable_Various_Various Configure_File_SaveCopyAdvanced_SaveAsCopyByExpressionSetDisplayName(EditorExecuteState state) => state.NEFiles.FilesWindow.RunDialog_Configure_FileTable_Various_Various(state.NEFiles.Focused.GetVariables(), state.NEFiles.Focused.Selections.Count);

		void Execute_File_SaveCopy_SaveAsCopyByExpression(bool copyOnly = false)
		{
			var result = state.Configuration as Configuration_FileTable_Various_Various;
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

		static Configuration_FileTable_Various_Various Configure_File_Move_MoveByExpression(EditorExecuteState state) => state.NEFiles.FilesWindow.RunDialog_Configure_FileTable_Various_Various(state.NEFiles.Focused.GetVariables(), state.NEFiles.Focused.Selections.Count);

		void Execute_File_Move_MoveByExpression()
		{
			var result = state.Configuration as Configuration_FileTable_Various_Various;
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

		static Configuration_File_OpenEncoding_ReopenWithEncoding Configure_File_Encoding(EditorExecuteState state) => state.NEFiles.FilesWindow.RunDialog_Configure_File_OpenEncoding_ReopenWithEncoding(state.NEFiles.Focused.CodePage);

		void Execute_File_Encoding()
		{
			var result = state.Configuration as Configuration_File_OpenEncoding_ReopenWithEncoding;
			CodePage = result.CodePage;
		}

		static Configuration_File_LineEndings Configure_File_LineEndings(EditorExecuteState state)
		{
			var endings = state.NEFiles.ActiveFiles.Select(neFile => neFile.Text.OnlyEnding).Distinct().Take(2).ToList();
			var ending = endings.Count == 1 ? endings[0] : "";
			return state.NEFiles.FilesWindow.RunDialog_Configure_File_LineEndings(ending);
		}

		void Execute_File_LineEndings()
		{
			var result = state.Configuration as Configuration_File_LineEndings;
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

		void Execute_File_Advanced_Compress() => Compressed = state.MultiStatus == false;

		static Configuration_File_Advanced_Encrypt Configure_File_Advanced_Encrypt(EditorExecuteState state)
		{
			if (state.MultiStatus != false)
				return new Configuration_File_Advanced_Encrypt();
			else
				return state.NEFiles.FilesWindow.RunDialog_Configure_File_Advanced_Encrypt(Cryptor.Type.AES, true);
		}

		void Execute_File_Advanced_Encrypt() => AESKey = (state.Configuration as Configuration_File_Advanced_Encrypt).Key;

		void Execute_File_Advanced_Explore() => Process.Start("explorer.exe", $"/select,\"{FileName}\"");

		void Execute_File_Advanced_CommandPrompt() => Process.Start(new ProcessStartInfo("cmd.exe") { WorkingDirectory = Path.GetDirectoryName(FileName) });

		void Execute_File_Advanced_DragDrop() => newDragFiles.Add(FileName);

		void Execute_File_Advanced_SetDisplayName()
		{
			var result = state.Configuration as Configuration_FileTable_Various_Various;
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

		static PreExecutionStop PreExecute_File_Advanced_DontExitOnClose(EditorExecuteState state)
		{
			Settings.DontExitOnClose = state.MultiStatus != true;
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_File_Close_ActiveInactiveFiles(EditorExecuteState state, bool active)
		{
			foreach (var neFile in (active ? state.NEFiles.ActiveFiles : state.NEFiles.AllFiles.Except(state.NEFiles.ActiveFiles)))
			{
				neFile.VerifyCanClose();
				state.NEFiles.RemoveFile(neFile);
			}

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_File_Close_FilesWithWithoutSelections(EditorExecuteState state, bool hasSelections)
		{
			foreach (var neFile in state.NEFiles.ActiveFiles.Where(neFile => neFile.Selections.Any() == hasSelections))
			{
				neFile.VerifyCanClose();
				state.NEFiles.RemoveFile(neFile);
			}

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_File_Close_ModifiedUnmodifiedFiles(EditorExecuteState state, bool modified)
		{
			foreach (var neFile in state.NEFiles.ActiveFiles.Where(neFile => neFile.IsModified == modified))
			{
				neFile.VerifyCanClose();
				state.NEFiles.RemoveFile(neFile);
			}

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_File_Exit(EditorExecuteState state)
		{
			foreach (var neFile in state.NEFiles.AllFiles)
			{
				state.NEFiles.AddToTransaction(neFile);
				neFile.VerifyCanClose();
				state.NEFiles.RemoveFile(neFile);
			}
			NEFiles.Instances.Remove(state.NEFiles);
			state.NEFiles.FilesWindow.CloseWindow();

			if (!NEFiles.Instances.Any())
			{
				if (((state.Configuration as Configuration_File_Exit)?.WindowClosed != true) || (!Settings.DontExitOnClose))
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

			return PreExecutionStop.Stop;
		}
	}
}
