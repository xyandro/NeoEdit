﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.Editor.PreExecution;

namespace NeoEdit.Editor
{
	partial class NEFile
	{
		string GetSelectionsName()
		{
			if (!string.IsNullOrWhiteSpace(DisplayName))
				return $"Selections for {DisplayName}";
			if (!string.IsNullOrWhiteSpace(FileName))
				return $"Selections for {Path.GetFileName(FileName)}";
			return null;
		}

		public static NEFile CreateFileFromStrings(IReadOnlyList<string> strs, string name, ParserType contentType)
		{
			var sb = new StringBuilder(strs.Sum(str => str.Length + 2)); // 2 is for ending; may or may not need it
			var sels = new List<NERange>();
			foreach (var str in strs)
			{
				var start = sb.Length;
				sb.Append(str);
				sels.Add(new NERange(start, sb.Length));
				if ((!str.EndsWith("\r")) && (!str.EndsWith("\n")))
					sb.Append("\r\n");
			}

			return new NEFile(displayName: name, bytes: Coder.StringToBytes(sb.ToString(), Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, contentType: contentType, modified: false)
			{
				Selections = sels,
				StartRow = 0,
				StartColumn = 0,
			};
		}

		string GetSaveFileName()
		{
			return NEWindow.ShowFile(this, () =>
			{
				var result = NEWindow.neWindowUI.RunSaveFileDialog(Path.GetFileName(FileName) ?? DisplayName, "txt", Path.GetDirectoryName(FileName), "All files|*.*");
				if (Directory.Exists(result.FileName))
					throw new Exception("A directory by that name already exists");
				if (!Directory.Exists(Path.GetDirectoryName(result.FileName)))
					throw new Exception("Directory doesn't exist");
				return result.FileName;
			});
		}

		void Execute_File_New_FromSelections_AllFilesSelections()
		{
			(state.PreExecution as PreExecution_File_New_FromSelections_AllFilesSelections).Selections[this] = (GetSelectionStrings(), GetSelectionsName(), ContentType);
		}

		void Execute_File_Open_ReopenWithEncoding()
		{
			var result = state.Configuration as Configuration_File_OpenEncoding_ReopenWithEncoding;
			if (!CheckModified(MessageOptions.Yes))
				return;

			OpenFile(FileName, codePage: result.CodePage);
		}

		public void Execute_File_Refresh()
		{
			if ((string.IsNullOrEmpty(FileName)) || (LastWriteTime == LastExternalWriteTime))
				return;

			if (File.Exists(FileName))
			{
				if (QueryUser($"{nameof(Execute_File_Refresh)}-Updated", "This file has been updated on disk. Reload?", MessageOptions.Yes))
					Execute_File_Revert();
			}
			else
			{
				QueryUser($"{nameof(Execute_File_Refresh)}-Deleted", "This file has been deleted.", MessageOptions.Ok, MessageOptions.OkAllCancel);
			}
		}

		void Execute_File_AutoRefresh() => AutoRefresh = state.MultiStatus != true;

		void Execute_File_Revert()
		{
			if (!CheckModified(MessageOptions.No))
				return;

			var selections = Selections.ToList();
			var regions = Enumerable.Range(1, 9).ToDictionary(index => index, index => GetRegions(index));

			OpenFile(FileName, DisplayName);

			Func<IReadOnlyList<NERange>, IReadOnlyList<NERange>> reformatRanges = l => l.Select(range => new NERange(Math.Max(0, Math.Min(range.Anchor, Text.Length)), Math.Max(0, Math.Min(range.Cursor, Text.Length)))).ToList();
			Selections = reformatRanges(selections);
			for (var region = 1; region <= 9; ++region)
				SetRegions(region, reformatRanges(GetRegions(region)));
		}

		void Execute_File_Save_SaveModified()
		{
			if ((FileName != null) && (!IsModified) && (LastWriteTime == LastExternalWriteTime))
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

			watcher.EnableRaisingEvents = false;
			File.Delete(FileName);
			LastWriteTime = LastExternalWriteTime = LastActivatedTime = DateTime.MinValue;
			watcher.EnableRaisingEvents = true;
		}

		void Execute_File_Encoding()
		{
			var result = state.Configuration as Configuration_File_OpenEncoding_ReopenWithEncoding;
			CodePage = result.CodePage;
			HasBOM = result.HasBOM;
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
			var sel = new List<NERange>();
			for (var line = 0; line < lines; ++line)
			{
				var current = Text.GetEnding(line);
				if ((current.Length == 0) || (current == result.LineEndings))
					continue;
				var start = Text.GetPosition(line, Text.GetLineLength(line));
				sel.Add(NERange.FromIndex(start, current.Length));
			}
			Replace(sel, sel.Select(str => result.LineEndings).ToList());
		}

		void Execute_File_FileActiveFileIndex(bool activeOnly)
		{
			ReplaceSelections((NEWindow.GetFileIndex(this, activeOnly) + 1).ToString());
		}

		void Execute_File_Advanced_Compress() => Compressed = state.MultiStatus != true;

		static void Configure_File_Advanced_Encrypt()
		{
			if (state.MultiStatus != true)
				state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_File_Advanced_Encrypt(Cryptor.Type.AES, true);
			else
				state.Configuration = new Configuration_File_Advanced_Encrypt();
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

		static void PreExecute_File_Advanced_DontExitOnClose()
		{
			Settings.DontExitOnClose = state.MultiStatus != true;
			state.PreExecution = PreExecution_TaskFinished.Singleton;
		}

		static void PreExecute_File_Close_ActiveInactiveFiles(bool active)
		{
			foreach (var neFile in (active ? state.NEWindow.ActiveFiles : state.NEWindow.NEFiles.Except(state.NEWindow.ActiveFiles)))
			{
				neFile.VerifyCanClose();
				neFile.Close();
			}

			state.PreExecution = PreExecution_TaskFinished.Singleton;
		}

		void Execute_File_Close_FilesWithWithoutSelections(bool hasSelections)
		{
			if (Selections.Any() == hasSelections)
			{
				VerifyCanClose();
				Close();
			}
		}

		void Execute_File_Close_ModifiedUnmodifiedFiles(bool modified)
		{
			if (IsModified == modified)
			{
				VerifyCanClose();
				Close();
			}
		}

		void Execute_File_Close_ExternalModifiedUnmodifiedFiles(bool modified)
		{
			if ((LastWriteTime != LastExternalWriteTime) == modified)
			{
				VerifyCanClose();
				Close();
			}
		}

		static void Configure_File_Exit()
		{
			state.Configuration = new Configuration_File_Exit { ShouldExit = true };
		}

		static void PreExecute_File_Exit()
		{
			foreach (var neFile in state.NEWindow.NEFiles)
			{
				neFile.VerifyCanClose();
				neFile.Close();
			}
			state.NEWindow.ClearNEWindows();

			state.PreExecution = PreExecution_TaskFinished.Singleton;
		}
	}
}
