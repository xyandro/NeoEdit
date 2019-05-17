using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Win32;
using NeoEdit.Common;
using NeoEdit.Common.Controls;
using NeoEdit.Common.Dialogs;
using NeoEdit.Common.NEClipboards;
using NeoEdit.Common.Transform;
using NeoEdit.MenuFile.Dialogs;

namespace NeoEdit.MenuFile
{
	public static class FileFunctions
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

		static public void Command_File_New_New(ITabs tabs, bool createTabs) => (createTabs ? ITabsCreator.CreateTabs() : tabs).Add();

		static public void Command_File_New_FromSelections(ITextEditor te) => te.GetSelectionStrings().ForEach((str, index) => te.TabsParent.Add(displayName: $"Selection {index + 1}", bytes: Coder.StringToBytes(str, Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, contentType: te.ContentType, modified: false));

		static public void Command_File_New_FromClipboards(ITabs tabs) => NEClipboard.Current.Strings.ForEach((str, index) => tabs.Add(displayName: $"Clipboard {index + 1}", bytes: Coder.StringToBytes(str, Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, modified: false));

		static public OpenFileDialogResult Command_File_Open_Open_Dialog(ITabs tabs, string initialDirectory = null)
		{
			if ((initialDirectory == null) && (tabs.TopMost != null))
				initialDirectory = Path.GetDirectoryName(tabs.TopMost.FileName);
			var dialog = new OpenFileDialog
			{
				DefaultExt = "txt",
				Filter = "Text files|*.txt|All files|*.*",
				FilterIndex = 2,
				Multiselect = true,
				InitialDirectory = initialDirectory,
			};
			if (dialog.ShowDialog() != true)
				return null;

			return new OpenFileDialogResult { files = dialog.FileNames.ToList() };
		}

		static public void Command_File_Open_Open(ITabs tabs, OpenFileDialogResult result) => result.files.ForEach(fileName => tabs.Add(fileName));

		static public void Command_File_Open_CopiedCut(ITabs tabs)
		{
			var files = NEClipboard.Current.Strings;

			if ((files.Count > 5) && (new Message(tabs.WindowParent)
			{
				Title = "Confirm",
				Text = $"Are you sure you want to open these {files.Count} files?",
				Options = MessageOptions.YesNoCancel,
				DefaultAccept = MessageOptions.Yes,
				DefaultCancel = MessageOptions.Cancel,
			}.Show() != MessageOptions.Yes))
				return;

			foreach (var item in tabs.Items)
				item.Active = false;
			foreach (var file in files)
				tabs.Add(file);
		}

		static public void Command_File_Open_Selected(ITextEditor te)
		{
			var files = te.RelativeSelectedFiles();
			foreach (var file in files)
				te.TabsParent.Add(file);
		}

		static public void Command_File_Save_Save(ITextEditor te)
		{
			if (te.FileName == null)
				Command_File_Save_SaveAs(te);
			else
				te.Save(te.FileName);
		}

		static public void Command_File_Save_SaveAs(ITextEditor te, bool copyOnly = false) => te.Save(GetSaveFileName(te), copyOnly);

		static public GetExpressionDialog.Result Command_File_Save_SaveAsByExpression_Dialog(ITextEditor te) => GetExpressionDialog.Run(te.WindowParent, te.GetVariables(), te.Selections.Count);

		static public void Command_File_Save_SaveAsByExpression(ITextEditor te, GetExpressionDialog.Result result, AnswerResult answer, bool copyOnly = false)
		{
			var results = te.GetFixedExpressionResults<string>(result.Expression);
			if (results.Count != 1)
				throw new Exception("Only one filename may be specified");

			var newFileName = te.FileName.RelativeChild(results[0]);

			if (File.Exists(newFileName))
			{
				if ((answer.Answer != MessageOptions.YesToAll) && (answer.Answer != MessageOptions.NoToAll))
					answer.Answer = new Message(te.WindowParent)
					{
						Title = "Confirm",
						Text = "File already exists; overwrite?",
						Options = MessageOptions.YesNoYesAllNoAllCancel,
						DefaultCancel = MessageOptions.Cancel,
					}.Show();

				if ((answer.Answer != MessageOptions.Yes) && (answer.Answer != MessageOptions.YesToAll))
					return;
			}

			te.Save(newFileName, copyOnly);
		}

		static public void Command_File_Save_SetDisplayName(ITextEditor te, GetExpressionDialog.Result result)
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

		static public void Command_File_Operations_Rename(ITextEditor te)
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

		static public GetExpressionDialog.Result Command_File_Operations_RenameByExpression_Dialog(ITextEditor te) => GetExpressionDialog.Run(te.WindowParent, te.GetVariables(), te.Selections.Count);

		static public void Command_File_Operations_RenameByExpression(ITextEditor te, GetExpressionDialog.Result result, AnswerResult answer)
		{
			var results = te.GetFixedExpressionResults<string>(result.Expression);
			if (results.Count != 1)
				throw new Exception("Only one filename may be specified");

			var newFileName = te.FileName.RelativeChild(results[0]);

			if ((!string.Equals(newFileName, te.FileName, StringComparison.OrdinalIgnoreCase)) && (File.Exists(newFileName)))
			{
				if ((answer.Answer != MessageOptions.YesToAll) && (answer.Answer != MessageOptions.NoToAll))
					answer.Answer = new Message(te.WindowParent)
					{
						Title = "Confirm",
						Text = "File already exists; overwrite?",
						Options = MessageOptions.YesNoYesAllNoAllCancel,
						DefaultCancel = MessageOptions.Cancel,
					}.Show();

				if ((answer.Answer != MessageOptions.Yes) && (answer.Answer != MessageOptions.YesToAll))
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

		static public void Command_File_Operations_Delete(ITextEditor te, AnswerResult answer)
		{
			if (te.FileName == null)
				return;

			if ((answer.Answer != MessageOptions.YesToAll) && (answer.Answer != MessageOptions.NoToAll))
				answer.Answer = new Message(te.WindowParent)
				{
					Title = "Confirm",
					Text = "Are you sure you want to delete this file?",
					Options = MessageOptions.YesNoYesAllNoAll,
					DefaultAccept = MessageOptions.No,
					DefaultCancel = MessageOptions.NoToAll,
				}.Show();

			if ((answer.Answer != MessageOptions.Yes) && (answer.Answer != MessageOptions.YesToAll))
				return;

			File.Delete(te.FileName);
		}

		static public void Command_File_Operations_Explore(ITextEditor te) => Process.Start("explorer.exe", $"/select,\"{te.FileName}\"");

		static public void Command_File_Operations_CommandPrompt(ITextEditor te) => Process.Start(new ProcessStartInfo("cmd.exe") { WorkingDirectory = Path.GetDirectoryName(te.FileName) });

		static public void Command_File_Operations_DragDrop(ITextEditor te)
		{
			if (string.IsNullOrWhiteSpace(te.FileName))
				throw new Exception("No current file.");
			if (!File.Exists(te.FileName))
				throw new Exception("Current file does not exist.");
			te.doDrag = DragType.CurrentFile;
		}

		static public void Command_File_Operations_VCSDiff(ITextEditor te)
		{
			if (string.IsNullOrEmpty(te.FileName))
				throw new Exception("Must have filename to do diff");
			var original = Versioner.GetUnmodifiedFile(te.FileName);
			if (original == null)
				throw new Exception("Unable to get VCS content");

			var topMost = te.TabsParent.TopMost;
			var textEdit = te.TabsParent.Add(displayName: Path.GetFileName(te.FileName), modified: false, bytes: original, index: te.TabsParent.GetIndex(te));
			textEdit.ContentType = te.ContentType;
			textEdit.DiffTarget = te;
			te.TabsParent.TopMost = topMost;
		}

		static public void Command_File_Close(ITextEditor te, AnswerResult answer)
		{
			if (te.CanClose(answer))
				te.TabsParent.Remove(te);
		}

		static public void Command_File_Refresh(ITextEditor te, AnswerResult answer)
		{
			if (string.IsNullOrEmpty(te.FileName))
				return;

			if (!File.Exists(te.FileName))
				throw new Exception("This file has been deleted.");

			if (te.fileLastWrite != new FileInfo(te.FileName).LastWriteTime)
			{
				if ((answer.Answer != MessageOptions.YesToAll) && (answer.Answer != MessageOptions.NoToAll))
					answer.Answer = new Message(te.WindowParent)
					{
						Title = "Confirm",
						Text = "This file has been updated on disk.  Reload?",
						Options = MessageOptions.YesNoYesAllNoAll,
						DefaultAccept = MessageOptions.Yes,
						DefaultCancel = MessageOptions.NoToAll,
					}.Show();

				if ((answer.Answer != MessageOptions.Yes) && (answer.Answer != MessageOptions.YesToAll))
					return;

				Command_File_Revert(te, answer);
			}
		}

		static public void Command_File_AutoRefresh(ITextEditor te, bool? multiStatus) => te.SetAutoRefresh(multiStatus != true);

		static public void Command_File_Revert(ITextEditor te, AnswerResult answer)
		{
			if (te.IsModified)
			{
				if ((answer.Answer != MessageOptions.YesToAll) && (answer.Answer != MessageOptions.NoToAll))
					answer.Answer = new Message(te.WindowParent)
					{
						Title = "Confirm",
						Text = "You have unsaved changes.  Are you sure you want to reload?",
						Options = MessageOptions.YesNoYesAllNoAllCancel,
						DefaultAccept = MessageOptions.No,
						DefaultCancel = MessageOptions.No,
					}.Show();
				if ((answer.Answer != MessageOptions.Yes) && (answer.Answer != MessageOptions.YesToAll))
					return;
			}

			te.OpenFile(te.FileName, te.DisplayName, keepUndo: true);
		}

		static public void Command_File_MoveToNewWindow(ITabs tabs)
		{
			var active = tabs.Items.Where(tab => tab.Active).ToList();
			active.ForEach(tab => tabs.Items.Remove(tab));

			var newWindow = ITabsCreator.CreateTabs();
			newWindow.SetLayout(newWindow.Layout, newWindow.Columns, newWindow.Rows);
			active.ForEach(tab => newWindow.Add(tab));
		}

		static public void Command_File_Insert_Files(ITextEditor te)
		{
			if (te.Selections.Count != 1)
			{
				new Message(te.WindowParent)
				{
					Title = "Error",
					Text = "Must have one selection.",
					Options = MessageOptions.Ok,
				}.Show();
				return;
			}

			var dialog = new OpenFileDialog { DefaultExt = "txt", Filter = "Text files|*.txt|All files|*.*", FilterIndex = 2, Multiselect = true };
			if (dialog.ShowDialog() == true)
				InsertFiles(te, dialog.FileNames);
		}

		static public void Command_File_Insert_CopiedCut(ITextEditor te)
		{
			if (te.Selections.Count != 1)
			{
				new Message(te.WindowParent)
				{
					Title = "Error",
					Text = "Must have one selection.",
					Options = MessageOptions.Ok,
				}.Show();
				return;
			}

			var files = te.Clipboard;
			if (files.Count == 0)
				return;

			if ((files.Count > 5) && (new Message(te.WindowParent)
			{
				Title = "Confirm",
				Text = $"Are you sure you want to insert these {files.Count} files?",
				Options = MessageOptions.YesNoCancel,
				DefaultAccept = MessageOptions.Yes,
				DefaultCancel = MessageOptions.Cancel,
			}.Show() != MessageOptions.Yes))
				return;

			InsertFiles(te, files);
		}

		static public void Command_File_Insert_Selected(ITextEditor te) => InsertFiles(te, te.RelativeSelectedFiles());

		static public void Command_File_Copy_Path(ITextEditor te) => te.SetClipboardFile(te.FileName);

		static public void Command_File_Copy_Name(ITextEditor te) => te.SetClipboardString(Path.GetFileName(te.FileName));

		static public void Command_File_Copy_DisplayName(ITextEditor te) => te.SetClipboardString(te.DisplayName ?? Path.GetFileName(te.FileName));

		static public EncodingDialog.Result Command_File_Encoding_Encoding_Dialog(ITextEditor te) => EncodingDialog.Run(te.WindowParent, te.CodePage);

		static public void Command_File_Encoding_Encoding(ITextEditor te, EncodingDialog.Result result) => te.CodePage = result.CodePage;

		static public EncodingDialog.Result Command_File_Encoding_ReopenWithEncoding_Dialog(ITextEditor te) => EncodingDialog.Run(te.WindowParent, te.CodePage);

		static public void Command_File_Encoding_ReopenWithEncoding(ITextEditor te, EncodingDialog.Result result, AnswerResult answer)
		{
			if (te.IsModified)
			{
				if ((answer.Answer != MessageOptions.YesToAll) && (answer.Answer != MessageOptions.NoToAll))
					answer.Answer = new Message(te.WindowParent)
					{
						Title = "Confirm",
						Text = "You have unsaved changes.  Are you sure you want to reload?",
						Options = MessageOptions.YesNoYesAllNoAll,
						DefaultAccept = MessageOptions.Yes,
						DefaultCancel = MessageOptions.NoToAll,
					}.Show();
				if ((answer.Answer != MessageOptions.Yes) && (answer.Answer != MessageOptions.YesToAll))
					return;
			}

			te.OpenFile(te.FileName, codePage: result.CodePage);
		}

		static public FileEncodingLineEndingsDialog.Result Command_File_Encoding_LineEndings_Dialog(ITextEditor te) => FileEncodingLineEndingsDialog.Run(te.WindowParent, te.LineEnding ?? "");

		static public void Command_File_Encoding_LineEndings(ITextEditor te, FileEncodingLineEndingsDialog.Result result)
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

		static public string Command_File_Encrypt_Dialog(ITextEditor te, bool? multiStatus)
		{
			if (multiStatus != false)
				return "";

			return FileSaver.GetKey(te.WindowParent, true);
		}

		static public void Command_File_Encrypt(ITextEditor te, string result) => te.AESKey = result == "" ? null : result;

		static public void Command_File_Compress(ITextEditor te, bool? multiStatus) => te.Compressed = multiStatus == false;

		static public void Command_File_Shell_Integrate()
		{
			using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Default))
			using (var starKey = baseKey.OpenSubKey("*"))
			using (var shellKey = starKey.OpenSubKey("shell", true))
			using (var neoEditKey = shellKey.CreateSubKey("Open with NeoEdit Text Editor"))
			using (var commandKey = neoEditKey.CreateSubKey("command"))
				commandKey.SetValue("", $@"""{Assembly.GetEntryAssembly().Location}"" -text ""%1""");
		}

		static public void Command_File_Shell_Unintegrate()
		{
			using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Default))
			using (var starKey = baseKey.OpenSubKey("*"))
			using (var shellKey = starKey.OpenSubKey("shell", true))
				shellKey.DeleteSubKeyTree("Open with NeoEdit Text Editor");
		}
	}
}
