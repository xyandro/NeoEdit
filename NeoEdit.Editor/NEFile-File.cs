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
using NeoEdit.TaskRunning;

namespace NeoEdit.Editor
{
	partial class NEFile
	{
		static void AddFilesFromClipboards(NEWindow neWindow)
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
				var te = new NEFile(displayName: $"Clipboard {index}", bytes: Coder.StringToBytes(sb.ToString(), Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, modified: false);
				neWindow.AddNewNEFile(te);
				te.Selections = sels;
			}
		}

		static void AddFilesFromClipboardSelections(NEWindow neWindow) => NEClipboard.Current.Strings.ForEach((str, index) => neWindow.AddNewNEFile(new NEFile(displayName: $"Clipboard {index + 1}", bytes: Coder.StringToBytes(str, Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, modified: false)));

		string GetSaveFileName()
		{
			return state.NEWindow.ShowFile(this, () =>
			{
				var result = state.NEWindow.neWindowUI.RunSaveFileDialog(Path.GetFileName(FileName) ?? DisplayName, "txt", Path.GetDirectoryName(FileName), "All files|*.*");
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
			state.NEWindow.ActiveFiles = state.NEWindow.NEFiles;
			state.NEWindow.Focused = state.NEWindow.ActiveFiles.FirstOrDefault();
			return true;
		}

		static bool PreExecute_File_Select_None()
		{
			state.NEWindow.ClearActiveFiles();
			return true;
		}

		static bool PreExecute_File_Select_WithWithoutSelections(bool hasSelections)
		{
			state.NEWindow.SetActiveFiles(state.NEWindow.ActiveFiles.Where(neFile => neFile.Selections.Any() == hasSelections));
			return true;
		}

		static bool PreExecute_File_Select_ModifiedUnmodified(bool modified)
		{
			state.NEWindow.SetActiveFiles(state.NEWindow.ActiveFiles.Where(neFile => neFile.IsModified == modified));
			return true;
		}

		static bool PreExecute_File_Select_Inactive()
		{
			state.NEWindow.SetActiveFiles(state.NEWindow.NEFiles.Except(state.NEWindow.ActiveFiles));
			return true;
		}

		static bool PreExecute_File_Select_Choose()
		{
			var data = new WindowActiveFilesDialogData();
			void RecalculateData()
			{
				data.AllFiles = state.NEWindow.NEFiles.Select(neFile => neFile.NEFileLabel).ToList();
				data.ActiveIndexes = state.NEWindow.ActiveFiles.Select(neFile => state.NEWindow.NEFiles.IndexOf(neFile)).ToList();
				data.FocusedIndex = state.NEWindow.NEFiles.IndexOf(state.NEWindow.Focused);
			}
			RecalculateData();
			data.SetActiveIndexes = list =>
			{
				//state.NEWindow.ClearAllActive();
				//list.Select(index => state.NEWindow.AllFiles[index]).ForEach(neFile => state.NEWindow.SetActive(neFile));
				//RecalculateData();
				//state.NEWindow.RenderFilesWindow();
			};
			data.CloseFiles = list =>
			{
				//var neWindow = list.Select(index => state.NEWindow.AllFiles[index]).ToList();
				//neWindow.ForEach(neFile => neFile.VerifyCanClose());
				//neWindow.ForEach(neFile => state.NEWindow.RemoveFile(neFile));
				//RecalculateData();
				//state.NEWindow.RenderFilesWindow();
			};
			data.DoMoves = moves =>
			{
				//moves.ForEach(((int oldIndex, int newIndex) move) => state.NEWindow.MoveFile(state.NEWindow.AllFiles[move.oldIndex], move.newIndex));
				//RecalculateData();
				//state.NEWindow.RenderFilesWindow();
			};

			state.NEWindow.neWindowUI.RunDialog_PreExecute_File_Select_Choose(data);

			return true;
		}

		static bool PreExecute_File_New_New()
		{
			state.NEWindow.AddNewNEFile(new NEFile());
			return true;
		}

		static bool PreExecute_File_New_FromClipboard_Selections()
		{
			AddFilesFromClipboardSelections(state.NEWindow);
			return true;
		}

		static bool PreExecute_File_New_FromClipboard_Files()
		{
			AddFilesFromClipboards(state.NEWindow);
			return true;
		}

		static bool PreExecute_File_New_WordList()
		{
			byte[] data;
			var streamName = typeof(NEWindow).Assembly.GetManifestResourceNames().Where(name => name.EndsWith(".Words.txt.gz")).Single();
			using (var stream = typeof(NEWindow).Assembly.GetManifestResourceStream(streamName))
			using (var ms = new MemoryStream())
			{
				stream.CopyTo(ms);
				data = ms.ToArray();
			}

			data = Compressor.Decompress(data, Compressor.Type.GZip);
			data = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(data));
			state.NEWindow.AddNewNEFile(new NEFile(displayName: "Word List", bytes: data, modified: false));

			return true;
		}

		static void Configure_FileMacro_Open_Open(string initialDirectory = null)
		{
			if ((initialDirectory == null) && (state.NEWindow.Focused != null))
				initialDirectory = Path.GetDirectoryName(state.NEWindow.Focused.FileName);
			var result = state.NEWindow.neWindowUI.RunDialog_Configure_FileMacro_Open_Open("txt", initialDirectory, "Text files|*.txt|All files|*.*", 2, true);
			if (result == null)
				throw new OperationCanceledException();
			state.Configuration = result;
		}

		static bool PreExecute_FileMacro_Open_Open()
		{
			var result = state.Configuration as Configuration_FileMacro_Open_Open;
			result.FileNames.ForEach(fileName => state.NEWindow.AddNewNEFile(new NEFile(fileName)));
			return true;
		}

		static bool PreExecute_File_Open_CopiedCut()
		{
			NEClipboard.Current.Strings.AsTaskRunner().Select(file => new NEFile(file)).ForEach(neFile => state.NEWindow.AddNewNEFile(neFile));
			return true;
		}

		static void Configure_File_Open_ReopenWithEncoding() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_File_OpenEncoding_ReopenWithEncoding(state.NEWindow.Focused.CodePage);

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

			OpenFile(FileName, DisplayName);

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

		static void Configure_File_SaveCopyAdvanced_SaveAsCopyByExpressionSetDisplayName() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_FileTable_Various_Various(state.NEWindow.Focused.GetVariables(), state.NEWindow.Focused.Selections.Count);

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

		static void Configure_File_Move_MoveByExpression() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_FileTable_Various_Various(state.NEWindow.Focused.GetVariables(), state.NEWindow.Focused.Selections.Count);

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

		static void Configure_File_Encoding() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_File_OpenEncoding_ReopenWithEncoding(state.NEWindow.Focused.CodePage);

		void Execute_File_Encoding()
		{
			var result = state.Configuration as Configuration_File_OpenEncoding_ReopenWithEncoding;
			CodePage = result.CodePage;
		}

		static void Configure_File_LineEndings()
		{
			var endings = state.NEWindow.ActiveFiles.Select(neFile => neFile.Text.OnlyEnding).Distinct().Take(2).ToList();
			var ending = endings.Count == 1 ? endings[0] : "";
			state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_File_LineEndings(ending);
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
			ReplaceSelections((state.NEWindow.GetFileIndex(this, activeOnly) + 1).ToString());
		}

		void Execute_File_Advanced_Compress() => Compressed = state.MultiStatus == false;

		static void Configure_File_Advanced_Encrypt()
		{
			if (state.MultiStatus != false)
				state.Configuration = new Configuration_File_Advanced_Encrypt();
			else
				state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_File_Advanced_Encrypt(Cryptor.Type.AES, true);
		}

		void Execute_File_Advanced_Encrypt() => AESKey = (state.Configuration as Configuration_File_Advanced_Encrypt).Key;

		void Execute_File_Advanced_Explore() => Process.Start("explorer.exe", $"/select,\"{FileName}\"");

		void Execute_File_Advanced_CommandPrompt() => Process.Start(new ProcessStartInfo("cmd.exe") { WorkingDirectory = Path.GetDirectoryName(FileName) });

		void Execute_File_Advanced_DragDrop() => AddDragFile(FileName);

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

		static bool PreExecute_File_Advanced_DontExitOnClose()
		{
			Settings.DontExitOnClose = state.MultiStatus != true;
			return true;
		}

		static bool PreExecute_File_Close_ActiveInactiveFiles(bool active)
		{
			foreach (var neFile in (active ? state.NEWindow.ActiveFiles : state.NEWindow.NEFiles.Except(state.NEWindow.ActiveFiles)))
			{
				neFile.VerifyCanClose();
				neFile.ClearNEFiles();
			}

			return true;
		}

		void Execute_File_Close_FilesWithWithoutSelections(bool hasSelections)
		{
			if (Selections.Any() == hasSelections)
			{
				VerifyCanClose();
				ClearNEFiles();
			}
		}

		void Execute_File_Close_ModifiedUnmodifiedFiles(bool modified)
		{
			if (IsModified == modified)
			{
				VerifyCanClose();
				ClearNEFiles();
			}
		}

		static bool PreExecute_File_Exit()
		{
			foreach (var neFile in state.NEWindow.NEFiles)
			{
				neFile.VerifyCanClose();
				neFile.ClearNEFiles();
			}
			state.NEGlobal.RemoveNEWindow(state.NEWindow);

			return true;
		}
	}
}
