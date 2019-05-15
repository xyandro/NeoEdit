using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using NeoEdit.Controls;
using NeoEdit.Dialogs;
using NeoEdit.Misc;
using NeoEdit.Transform;

namespace NeoEdit
{
	partial class TextEditor
	{
		static string GetSaveFileName(ITextEditor te)
		{
			var dialog = new SaveFileDialog
			{
				Filter = "All files|*.*",
				FileName = Path.GetFileName(te.FileName) ?? te.DisplayName,
				InitialDirectory = Path.GetDirectoryName(te.FileName),
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

		static void InsertFiles(ITextEditor te, IEnumerable<string> fileNames)
		{
			if ((te.Selections.Count != 1) && (te.Selections.Count != fileNames.Count()))
				throw new Exception("Must have either one or equal number of selections.");

			var strs = new List<string>();
			foreach (var fileName in fileNames)
			{
				var bytes = File.ReadAllBytes(fileName);
				strs.Add(Coder.BytesToString(bytes, Coder.CodePage.AutoByBOM, true));
			}

			if (te.Selections.Count == 1)
				te.ReplaceOneWithMany(strs, false);
			if (te.Selections.Count == fileNames.Count())
				te.ReplaceSelections(strs);
		}

		static void Command_File_New_FromSelections(ITextEditor te) => te.GetSelectionStrings().ForEach((str, index) => te.TabsParent.Add(new TextEditor(displayName: $"Selection {index + 1}", bytes: Coder.StringToBytes(str, Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, contentType: te.ContentType, modified: false)));

		static void Command_File_Open_Selected(ITextEditor te)
		{
			var files = te.RelativeSelectedFiles();
			foreach (var file in files)
				te.TabsParent.Add(new TextEditor(file));
		}

		static void Command_File_Save_Save(ITextEditor te)
		{
			if (te.FileName == null)
				Command_File_Save_SaveAs(te);
			else
				te.Save(te.FileName);
		}

		static void Command_File_Save_SaveAs(ITextEditor te, bool copyOnly = false) => te.Save(GetSaveFileName(te), copyOnly);

		static GetExpressionDialog.Result Command_File_Save_SaveAsByExpression_Dialog(ITextEditor te) => GetExpressionDialog.Run(te.TabsParent, te.GetVariables(), te.Selections.Count);

		static void Command_File_Save_SaveAsByExpression(ITextEditor te, GetExpressionDialog.Result result, AnswerResult answer, bool copyOnly = false)
		{
			var results = te.GetFixedExpressionResults<string>(result.Expression);
			if (results.Count != 1)
				throw new Exception("Only one filename may be specified");

			var newFileName = te.FileName.RelativeChild(results[0]);

			if (File.Exists(newFileName))
			{
				if ((answer.Answer != Message.OptionsEnum.YesToAll) && (answer.Answer != Message.OptionsEnum.NoToAll))
					answer.Answer = new Message(te.TabsParent)
					{
						Title = "Confirm",
						Text = "File already exists; overwrite?",
						Options = Message.OptionsEnum.YesNoYesAllNoAllCancel,
						DefaultCancel = Message.OptionsEnum.Cancel,
					}.Show();

				if ((answer.Answer != Message.OptionsEnum.Yes) && (answer.Answer != Message.OptionsEnum.YesToAll))
					return;
			}

			te.Save(newFileName, copyOnly);
		}

		static void Command_File_Save_SetDisplayName(ITextEditor te, GetExpressionDialog.Result result)
		{
			if (result.Expression == "f")
			{
				te.DisplayName = null;
				return;
			}
			var results = te.GetVariableExpressionResults<string>(result.Expression);
			if (results.Count != 1)
				throw new Exception("Only one value may be specified");
			te.DisplayName = results[0];
		}

		static void Command_File_Operations_Rename(ITextEditor te)
		{
			if (string.IsNullOrEmpty(te.FileName))
			{
				Command_File_Save_SaveAs(te);
				return;
			}

			var fileName = GetSaveFileName(te);

			if (!string.Equals(te.FileName, fileName, StringComparison.OrdinalIgnoreCase))
				File.Delete(fileName);
			File.Move(te.FileName, fileName);
			te.SetFileName(fileName);
		}

		static GetExpressionDialog.Result Command_File_Operations_RenameByExpression_Dialog(ITextEditor te) => GetExpressionDialog.Run(te.TabsParent, te.GetVariables(), te.Selections.Count);

		static void Command_File_Operations_RenameByExpression(ITextEditor te, GetExpressionDialog.Result result, AnswerResult answer)
		{
			var results = te.GetFixedExpressionResults<string>(result.Expression);
			if (results.Count != 1)
				throw new Exception("Only one filename may be specified");

			var newFileName = te.FileName.RelativeChild(results[0]);

			if ((!string.Equals(newFileName, te.FileName, StringComparison.OrdinalIgnoreCase)) && (File.Exists(newFileName)))
			{
				if ((answer.Answer != Message.OptionsEnum.YesToAll) && (answer.Answer != Message.OptionsEnum.NoToAll))
					answer.Answer = new Message(te.TabsParent)
					{
						Title = "Confirm",
						Text = "File already exists; overwrite?",
						Options = Message.OptionsEnum.YesNoYesAllNoAllCancel,
						DefaultCancel = Message.OptionsEnum.Cancel,
					}.Show();

				if ((answer.Answer != Message.OptionsEnum.Yes) && (answer.Answer != Message.OptionsEnum.YesToAll))
					return;
			}

			if (te.FileName != null)
			{
				if (!string.Equals(te.FileName, newFileName, StringComparison.OrdinalIgnoreCase))
					File.Delete(newFileName);
				File.Move(te.FileName, newFileName);
			}
			te.SetFileName(newFileName);
		}

		static void Command_File_Operations_Delete(ITextEditor te, AnswerResult answer)
		{
			if (te.FileName == null)
				return;

			if ((answer.Answer != Message.OptionsEnum.YesToAll) && (answer.Answer != Message.OptionsEnum.NoToAll))
				answer.Answer = new Message(te.TabsParent)
				{
					Title = "Confirm",
					Text = "Are you sure you want to delete this file?",
					Options = Message.OptionsEnum.YesNoYesAllNoAll,
					DefaultAccept = Message.OptionsEnum.No,
					DefaultCancel = Message.OptionsEnum.NoToAll,
				}.Show();

			if ((answer.Answer != Message.OptionsEnum.Yes) && (answer.Answer != Message.OptionsEnum.YesToAll))
				return;

			File.Delete(te.FileName);
		}

		static void Command_File_Operations_Explore(ITextEditor te) => Process.Start("explorer.exe", $"/select,\"{te.FileName}\"");

		static void Command_File_Operations_CommandPrompt(ITextEditor te) => Process.Start(new ProcessStartInfo("cmd.exe") { WorkingDirectory = Path.GetDirectoryName(te.FileName) });

		static void Command_File_Operations_DragDrop(ITextEditor te)
		{
			if (string.IsNullOrWhiteSpace(te.FileName))
				throw new Exception("No current file.");
			if (!File.Exists(te.FileName))
				throw new Exception("Current file does not exist.");
			te.doDrag = DragType.CurrentFile;
		}

		static void Command_File_Operations_VCSDiff(ITextEditor te)
		{
			if (string.IsNullOrEmpty(te.FileName))
				throw new Exception("Must have filename to do diff");
			var original = Versioner.GetUnmodifiedFile(te.FileName);
			if (original == null)
				throw new Exception("Unable to get VCS content");

			var topMost = te.TabsParent.TopMost;
			var textEdit = new TextEditor(displayName: Path.GetFileName(te.FileName), modified: false, bytes: original);
			textEdit.ContentType = te.ContentType;
			te.TabsParent.Add(textEdit, te.TabsParent.GetIndex(te as TextEditor));
			textEdit.DiffTarget = te as TextEditor;
			te.TabsParent.TopMost = topMost;
		}

		static void Command_File_Refresh(ITextEditor te, AnswerResult answer)
		{
			if (string.IsNullOrEmpty(te.FileName))
				return;

			if (!File.Exists(te.FileName))
				throw new Exception("This file has been deleted.");

			if (te.fileLastWrite != new FileInfo(te.FileName).LastWriteTime)
			{
				if ((answer.Answer != Message.OptionsEnum.YesToAll) && (answer.Answer != Message.OptionsEnum.NoToAll))
					answer.Answer = new Message(te.TabsParent)
					{
						Title = "Confirm",
						Text = "This file has been updated on disk.  Reload?",
						Options = Message.OptionsEnum.YesNoYesAllNoAll,
						DefaultAccept = Message.OptionsEnum.Yes,
						DefaultCancel = Message.OptionsEnum.NoToAll,
					}.Show();

				if ((answer.Answer != Message.OptionsEnum.Yes) && (answer.Answer != Message.OptionsEnum.YesToAll))
					return;

				Command_File_Revert(te, answer);
			}
		}

		static void Command_File_AutoRefresh(ITextEditor te, bool? multiStatus) => te.SetAutoRefresh(multiStatus != true);

		static void Command_File_Revert(ITextEditor te, AnswerResult answer)
		{
			if (te.IsModified)
			{
				if ((answer.Answer != Message.OptionsEnum.YesToAll) && (answer.Answer != Message.OptionsEnum.NoToAll))
					answer.Answer = new Message(te.TabsParent)
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

			te.OpenFile(te.FileName, te.DisplayName, keepUndo: true);
		}

		static void Command_File_Insert_Files(ITextEditor te)
		{
			if (te.Selections.Count != 1)
			{
				new Message(te.TabsParent)
				{
					Title = "Error",
					Text = "Must have one selection.",
					Options = Message.OptionsEnum.Ok,
				}.Show();
				return;
			}

			var dialog = new OpenFileDialog { DefaultExt = "txt", Filter = "Text files|*.txt|All files|*.*", FilterIndex = 2, Multiselect = true };
			if (dialog.ShowDialog() == true)
				InsertFiles(te, dialog.FileNames);
		}

		static void Command_File_Insert_CopiedCut(ITextEditor te)
		{
			if (te.Selections.Count != 1)
			{
				new Message(te.TabsParent)
				{
					Title = "Error",
					Text = "Must have one selection.",
					Options = Message.OptionsEnum.Ok,
				}.Show();
				return;
			}

			var files = te.Clipboard;
			if (files.Count == 0)
				return;

			if ((files.Count > 5) && (new Message(te.TabsParent)
			{
				Title = "Confirm",
				Text = $"Are you sure you want to insert these {files.Count} files?",
				Options = Message.OptionsEnum.YesNoCancel,
				DefaultAccept = Message.OptionsEnum.Yes,
				DefaultCancel = Message.OptionsEnum.Cancel,
			}.Show() != Message.OptionsEnum.Yes))
				return;

			InsertFiles(te, files);
		}

		static void Command_File_Insert_Selected(ITextEditor te) => InsertFiles(te, te.RelativeSelectedFiles());

		static void Command_File_Copy_Path(ITextEditor te) => te.SetClipboardFile(te.FileName);

		static void Command_File_Copy_Name(ITextEditor te) => te.SetClipboardString(Path.GetFileName(te.FileName));

		static void Command_File_Copy_DisplayName(ITextEditor te) => te.SetClipboardString(te.DisplayName ?? Path.GetFileName(te.FileName));

		static EncodingDialog.Result Command_File_Encoding_Encoding_Dialog(ITextEditor te) => EncodingDialog.Run(te.TabsParent, te.CodePage);

		static void Command_File_Encoding_Encoding(ITextEditor te, EncodingDialog.Result result) => te.CodePage = result.CodePage;

		static EncodingDialog.Result Command_File_Encoding_ReopenWithEncoding_Dialog(ITextEditor te) => EncodingDialog.Run(te.TabsParent, te.CodePage);

		static void Command_File_Encoding_ReopenWithEncoding(ITextEditor te, EncodingDialog.Result result, AnswerResult answer)
		{
			if (te.IsModified)
			{
				if ((answer.Answer != Message.OptionsEnum.YesToAll) && (answer.Answer != Message.OptionsEnum.NoToAll))
					answer.Answer = new Message(te.TabsParent)
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

			te.OpenFile(te.FileName, codePage: result.CodePage);
		}

		static FileEncodingLineEndingsDialog.Result Command_File_Encoding_LineEndings_Dialog(ITextEditor te) => FileEncodingLineEndingsDialog.Run(te.TabsParent, te.LineEnding ?? "");

		static void Command_File_Encoding_LineEndings(ITextEditor te, FileEncodingLineEndingsDialog.Result result)
		{
			var lines = te.Data.NumLines;
			var sel = new List<Range>();
			for (var line = 0; line < lines; ++line)
			{
				var current = te.Data.GetEnding(line);
				if ((current.Length == 0) || (current == result.LineEndings))
					continue;
				var start = te.Data.GetOffset(line, te.Data.GetLineLength(line));
				sel.Add(Range.FromIndex(start, current.Length));
			}
			te.Replace(sel, sel.Select(str => result.LineEndings).ToList());
		}

		static string Command_File_Encrypt_Dialog(ITextEditor te, bool? multiStatus)
		{
			if (multiStatus != false)
				return "";

			return FileSaver.GetKey(te.TabsParent, true);
		}

		static void Command_File_Encrypt(ITextEditor te, string result) => te.AESKey = result == "" ? null : result;

		static void Command_File_Compress(ITextEditor te, bool? multiStatus) => te.Compressed = multiStatus == false;
	}
}
