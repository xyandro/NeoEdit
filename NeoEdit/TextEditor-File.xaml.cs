using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using NeoEdit;
using NeoEdit.Transform;
using NeoEdit.Controls;
using NeoEdit.Dialogs;
using NeoEdit.Misc;

namespace NeoEdit
{
	partial class TextEditor
	{
		string GetSaveFileName()
		{
			var dialog = new SaveFileDialog
			{
				Filter = "All files|*.*",
				FileName = Path.GetFileName(FileName) ?? DisplayName,
				InitialDirectory = Path.GetDirectoryName(FileName),
				DefaultExt = "txt",
			};
			if (dialog.ShowDialog() != true)
				throw new Exception("Canceled");

			if (Directory.Exists(dialog.FileName))
				throw new Exception("A directory by that name already exists");
			if (!Directory.Exists(Path.GetDirectoryName(dialog.FileName)))
				throw new Exception("Directory doesn't exist");
			return dialog.FileName;
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

		void Command_File_New_FromSelections() => GetSelectionStrings().ForEach((str, index) => TextEditTabs.Create(displayName: $"Selection {index + 1}", bytes: Coder.StringToBytes(str, Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, contentType: ContentType, modified: false));

		void Command_File_Open_Selected()
		{
			var files = RelativeSelectedFiles();
			foreach (var file in files)
				TextEditTabs.Create(file);
		}

		void Command_File_Save_Save()
		{
			if (FileName == null)
				Command_File_Save_SaveAs();
			else
				Save(FileName);
		}

		void Command_File_Save_SaveAs(bool copyOnly = false) => Save(GetSaveFileName(), copyOnly);

		GetExpressionDialog.Result Command_File_Save_SaveAsByExpression_Dialog() => GetExpressionDialog.Run(TabsParent, GetVariables(), Selections.Count);

		void Command_File_Save_SaveAsByExpression(GetExpressionDialog.Result result, AnswerResult answer, bool copyOnly = false)
		{
			var results = GetFixedExpressionResults<string>(result.Expression);
			if (results.Count != 1)
				throw new Exception("Only one filename may be specified");

			var newFileName = FileName.RelativeChild(results[0]);

			if (File.Exists(newFileName))
			{
				if ((answer.Answer != Message.OptionsEnum.YesToAll) && (answer.Answer != Message.OptionsEnum.NoToAll))
					answer.Answer = new Message(TabsParent)
					{
						Title = "Confirm",
						Text = "File already exists; overwrite?",
						Options = Message.OptionsEnum.YesNoYesAllNoAllCancel,
						DefaultCancel = Message.OptionsEnum.Cancel,
					}.Show();

				if ((answer.Answer != Message.OptionsEnum.Yes) && (answer.Answer != Message.OptionsEnum.YesToAll))
					return;
			}

			Save(newFileName, copyOnly);
		}

		void Command_File_Save_SetDisplayName(GetExpressionDialog.Result result)
		{
			if (result.Expression == "f")
			{
				DisplayName = null;
				return;
			}
			var results = GetVariableExpressionResults<string>(result.Expression);
			if (results.Count != 1)
				throw new Exception("Only one value may be specified");
			DisplayName = results[0];
		}

		void Command_File_Operations_Rename()
		{
			if (string.IsNullOrEmpty(FileName))
			{
				Command_File_Save_SaveAs();
				return;
			}

			var fileName = GetSaveFileName();

			if (!string.Equals(FileName, fileName, StringComparison.OrdinalIgnoreCase))
				File.Delete(fileName);
			File.Move(FileName, fileName);
			SetFileName(fileName);
		}

		GetExpressionDialog.Result Command_File_Operations_RenameByExpression_Dialog() => GetExpressionDialog.Run(TabsParent, GetVariables(), Selections.Count);

		void Command_File_Operations_RenameByExpression(GetExpressionDialog.Result result, AnswerResult answer)
		{
			var results = GetFixedExpressionResults<string>(result.Expression);
			if (results.Count != 1)
				throw new Exception("Only one filename may be specified");

			var newFileName = FileName.RelativeChild(results[0]);

			if ((!string.Equals(newFileName, FileName, StringComparison.OrdinalIgnoreCase)) && (File.Exists(newFileName)))
			{
				if ((answer.Answer != Message.OptionsEnum.YesToAll) && (answer.Answer != Message.OptionsEnum.NoToAll))
					answer.Answer = new Message(TabsParent)
					{
						Title = "Confirm",
						Text = "File already exists; overwrite?",
						Options = Message.OptionsEnum.YesNoYesAllNoAllCancel,
						DefaultCancel = Message.OptionsEnum.Cancel,
					}.Show();

				if ((answer.Answer != Message.OptionsEnum.Yes) && (answer.Answer != Message.OptionsEnum.YesToAll))
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

		void Command_File_Operations_Delete(AnswerResult answer)
		{
			if (FileName == null)
				return;

			if ((answer.Answer != Message.OptionsEnum.YesToAll) && (answer.Answer != Message.OptionsEnum.NoToAll))
				answer.Answer = new Message(TabsParent)
				{
					Title = "Confirm",
					Text = "Are you sure you want to delete this file?",
					Options = Message.OptionsEnum.YesNoYesAllNoAll,
					DefaultAccept = Message.OptionsEnum.No,
					DefaultCancel = Message.OptionsEnum.NoToAll,
				}.Show();

			if ((answer.Answer != Message.OptionsEnum.Yes) && (answer.Answer != Message.OptionsEnum.YesToAll))
				return;

			File.Delete(FileName);
		}

		void Command_File_Operations_Explore() => Process.Start("explorer.exe", $"/select,\"{FileName}\"");

		void Command_File_Operations_CommandPrompt() => Process.Start(new ProcessStartInfo("cmd.exe") { WorkingDirectory = Path.GetDirectoryName(FileName) });

		void Command_File_Operations_DragDrop()
		{
			if (string.IsNullOrWhiteSpace(FileName))
				throw new Exception("No current file.");
			if (!File.Exists(FileName))
				throw new Exception("Current file does not exist.");
			doDrag = DragType.CurrentFile;
		}

		void Command_File_Operations_VCSDiff()
		{
			if (string.IsNullOrEmpty(FileName))
				throw new Exception("Must have filename to do diff");
			var original = Versioner.GetUnmodifiedFile(FileName);
			if (original == null)
				throw new Exception("Unable to get VCS content");

			var topMost = TabsParent.TopMost;
			var textEdit = new TextEditor(displayName: Path.GetFileName(FileName), modified: false, bytes: original);
			textEdit.ContentType = ContentType;
			TabsParent.AddTab(textEdit, TabsParent.GetIndex(this));
			textEdit.DiffTarget = this;
			TabsParent.TopMost = topMost;
		}

		void Command_File_Refresh(AnswerResult answer)
		{
			if (string.IsNullOrEmpty(FileName))
				return;

			if (!File.Exists(FileName))
				throw new Exception("This file has been deleted.");

			if (fileLastWrite != new FileInfo(FileName).LastWriteTime)
			{
				if ((answer.Answer != Message.OptionsEnum.YesToAll) && (answer.Answer != Message.OptionsEnum.NoToAll))
					answer.Answer = new Message(TabsParent)
					{
						Title = "Confirm",
						Text = "This file has been updated on disk.  Reload?",
						Options = Message.OptionsEnum.YesNoYesAllNoAll,
						DefaultAccept = Message.OptionsEnum.Yes,
						DefaultCancel = Message.OptionsEnum.NoToAll,
					}.Show();

				if ((answer.Answer != Message.OptionsEnum.Yes) && (answer.Answer != Message.OptionsEnum.YesToAll))
					return;

				Command_File_Revert(answer);
			}
		}

		void Command_File_AutoRefresh(bool? multiStatus) => SetAutoRefresh(multiStatus != true);

		void Command_File_Revert(AnswerResult answer)
		{
			if (IsModified)
			{
				if ((answer.Answer != Message.OptionsEnum.YesToAll) && (answer.Answer != Message.OptionsEnum.NoToAll))
					answer.Answer = new Message(TabsParent)
					{
						Title = "Confirm",
						Text = "You have unsaved changes.  Are you sure you want to reload?",
						Options = Message.OptionsEnum.YesNoYesAllNoAllCancel,
						DefaultAccept = Message.OptionsEnum.No,
						DefaultCancel = Message.OptionsEnum.No,
					}.Show();
				if ((answer.Answer != Message.OptionsEnum.Yes) && (answer.Answer != Message.OptionsEnum.YesToAll))
					return;
			}

			OpenFile(FileName, DisplayName, keepUndo: true);
		}

		void Command_File_Insert_Files()
		{
			if (Selections.Count != 1)
			{
				new Message(TabsParent)
				{
					Title = "Error",
					Text = "Must have one selection.",
					Options = Message.OptionsEnum.Ok,
				}.Show();
				return;
			}

			var dialog = new OpenFileDialog { DefaultExt = "txt", Filter = "Text files|*.txt|All files|*.*", FilterIndex = 2, Multiselect = true };
			if (dialog.ShowDialog() == true)
				InsertFiles(dialog.FileNames);
		}

		void Command_File_Insert_CopiedCut()
		{
			if (Selections.Count != 1)
			{
				new Message(TabsParent)
				{
					Title = "Error",
					Text = "Must have one selection.",
					Options = Message.OptionsEnum.Ok,
				}.Show();
				return;
			}

			var files = Clipboard;
			if (files.Count == 0)
				return;

			if ((files.Count > 5) && (new Message(TabsParent)
			{
				Title = "Confirm",
				Text = $"Are you sure you want to insert these {files.Count} files?",
				Options = Message.OptionsEnum.YesNoCancel,
				DefaultAccept = Message.OptionsEnum.Yes,
				DefaultCancel = Message.OptionsEnum.Cancel,
			}.Show() != Message.OptionsEnum.Yes))
				return;

			InsertFiles(files);
		}

		void Command_File_Insert_Selected() => InsertFiles(RelativeSelectedFiles());

		void Command_File_Copy_Path() => SetClipboardFile(FileName);

		void Command_File_Copy_Name() => SetClipboardString(Path.GetFileName(FileName));

		void Command_File_Copy_DisplayName() => SetClipboardString(DisplayName ?? Path.GetFileName(FileName));

		EncodingDialog.Result Command_File_Encoding_Encoding_Dialog() => EncodingDialog.Run(TabsParent, CodePage);

		void Command_File_Encoding_Encoding(EncodingDialog.Result result) => CodePage = result.CodePage;

		EncodingDialog.Result Command_File_Encoding_ReopenWithEncoding_Dialog() => EncodingDialog.Run(TabsParent, CodePage);

		void Command_File_Encoding_ReopenWithEncoding(EncodingDialog.Result result, AnswerResult answer)
		{
			if (IsModified)
			{
				if ((answer.Answer != Message.OptionsEnum.YesToAll) && (answer.Answer != Message.OptionsEnum.NoToAll))
					answer.Answer = new Message(TabsParent)
					{
						Title = "Confirm",
						Text = "You have unsaved changes.  Are you sure you want to reload?",
						Options = Message.OptionsEnum.YesNoYesAllNoAll,
						DefaultAccept = Message.OptionsEnum.Yes,
						DefaultCancel = Message.OptionsEnum.NoToAll,
					}.Show();
				if ((answer.Answer != Message.OptionsEnum.Yes) && (answer.Answer != Message.OptionsEnum.YesToAll))
					return;
			}

			OpenFile(FileName, codePage: result.CodePage);
		}

		FileEncodingLineEndingsDialog.Result Command_File_Encoding_LineEndings_Dialog() => FileEncodingLineEndingsDialog.Run(TabsParent, LineEnding ?? "");

		void Command_File_Encoding_LineEndings(FileEncodingLineEndingsDialog.Result result)
		{
			var lines = Data.NumLines;
			var sel = new List<Range>();
			for (var line = 0; line < lines; ++line)
			{
				var current = Data.GetEnding(line);
				if ((current.Length == 0) || (current == result.LineEndings))
					continue;
				var start = Data.GetOffset(line, Data.GetLineLength(line));
				sel.Add(Range.FromIndex(start, current.Length));
			}
			Replace(sel, sel.Select(str => result.LineEndings).ToList());
		}

		string Command_File_Encrypt_Dialog(bool? multiStatus)
		{
			if (multiStatus != false)
				return "";

			return FileSaver.GetKey(TabsParent, true);
		}

		void Command_File_Encrypt(string result) => AESKey = result == "" ? null : result;

		void Command_File_Compress(bool? multiStatus) => Compressed = multiStatus == false;
	}
}
