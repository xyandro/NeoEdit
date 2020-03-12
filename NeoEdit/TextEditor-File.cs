using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Dialogs;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program
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
				throw new OperationCanceledException();

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

		void Command_File_New_FromSelections() => GetSelectionStrings().ForEach(((str, index) => state.TabsWindow.AddTextEditor(new TextEditor(displayName: $"Selection {index + 1}", bytes: Coder.StringToBytes(str, Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, contentType: ContentType, modified: false))));

		void Command_File_Open_Selected()
		{
			var files = RelativeSelectedFiles();
			foreach (var file in files)
				state.TabsWindow.AddTextEditor(new TextEditor(file));
		}

		void Command_File_Save_Save()
		{
			if (FileName == null)
				Command_File_SaveCopy_SaveCopy();
			else
				Save(FileName);
		}

		void Command_File_SaveCopy_SaveCopy(bool copyOnly = false) => Save(GetSaveFileName(), copyOnly);

		GetExpressionDialog.Result Command_File_SaveCopy_SaveCopyByExpression_Dialog() => GetExpressionDialog.Run(state.TabsWindow, GetVariables(), Selections.Count);

		void Command_File_SaveCopy_SaveCopyClipboard(bool copyOnly = false)
		{
			var results = Clipboard;
			if (results.Count != 1)
				throw new Exception("Only one filename may be specified");

			var newFileName = FileName.RelativeChild(results[0]);

			if (File.Exists(newFileName))
			{
				if (!savedAnswers[nameof(Command_File_SaveCopy_SaveCopyClipboard)].HasFlag(MessageOptions.All))
					savedAnswers[nameof(Command_File_SaveCopy_SaveCopyClipboard)] = new Message(state.TabsWindow)
					{
						Title = "Confirm",
						Text = "File already exists; overwrite?",
						Options = MessageOptions.YesNoAllCancel,
						DefaultCancel = MessageOptions.Cancel,
					}.Show();

				if (!savedAnswers[nameof(Command_File_SaveCopy_SaveCopyClipboard)].HasFlag(MessageOptions.Yes))
					return;
			}

			Save(newFileName, copyOnly);
		}

		void Command_File_SaveCopy_SaveCopyByExpression(GetExpressionDialog.Result result, bool copyOnly = false)
		{
			var results = GetExpressionResults<string>(result.Expression, Selections.Count());
			if (results.Count != 1)
				throw new Exception("Only one filename may be specified");

			var newFileName = FileName.RelativeChild(results[0]);

			if (File.Exists(newFileName))
			{
				if (!savedAnswers[nameof(Command_File_SaveCopy_SaveCopyByExpression)].HasFlag(MessageOptions.All))
					savedAnswers[nameof(Command_File_SaveCopy_SaveCopyByExpression)] = new Message(state.TabsWindow)
					{
						Title = "Confirm",
						Text = "File already exists; overwrite?",
						Options = MessageOptions.YesNoAllCancel,
						DefaultCancel = MessageOptions.Cancel,
					}.Show();

				if (!savedAnswers[nameof(Command_File_SaveCopy_SaveCopyByExpression)].HasFlag(MessageOptions.Yes))
					return;
			}

			Save(newFileName, copyOnly);
		}

		void Command_File_Copy_Path()
		{
			Clipboard = new List<string> { FileName };
			ClipboardIsCut = false;
		}

		void Command_File_Copy_Name() => Clipboard = new List<string> { Path.GetFileName(FileName) };

		void Command_File_Copy_DisplayName() => Clipboard = new List<string> { DisplayName ?? Path.GetFileName(FileName) };

		void Command_File_Operations_Rename()
		{
			if (string.IsNullOrEmpty(FileName))
			{
				Command_File_SaveCopy_SaveCopy();
				return;
			}

			var fileName = GetSaveFileName();

			if (!string.Equals(FileName, fileName, StringComparison.OrdinalIgnoreCase))
				File.Delete(fileName);
			File.Move(FileName, fileName);
			SetFileName(fileName);
		}

		GetExpressionDialog.Result Command_File_Operations_RenameByExpression_Dialog() => GetExpressionDialog.Run(state.TabsWindow, GetVariables(), Selections.Count);

		void Command_File_Operations_RenameClipboard()
		{
			var results = Clipboard;
			if (results.Count != 1)
				throw new Exception("Only one filename may be specified");

			var newFileName = FileName.RelativeChild(results[0]);

			if ((!string.Equals(newFileName, FileName, StringComparison.OrdinalIgnoreCase)) && (File.Exists(newFileName)))
			{
				if (!savedAnswers[nameof(Command_File_Operations_RenameClipboard)].HasFlag(MessageOptions.All))
					savedAnswers[nameof(Command_File_Operations_RenameClipboard)] = new Message(state.TabsWindow)
					{
						Title = "Confirm",
						Text = "File already exists; overwrite?",
						Options = MessageOptions.YesNoAllCancel,
						DefaultCancel = MessageOptions.Cancel,
					}.Show();

				if (!savedAnswers[nameof(Command_File_Operations_RenameClipboard)].HasFlag(MessageOptions.Yes))
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

		void Command_File_Operations_RenameByExpression(GetExpressionDialog.Result result)
		{
			var results = GetExpressionResults<string>(result.Expression, Selections.Count());
			if (results.Count != 1)
				throw new Exception("Only one filename may be specified");

			var newFileName = FileName.RelativeChild(results[0]);

			if ((!string.Equals(newFileName, FileName, StringComparison.OrdinalIgnoreCase)) && (File.Exists(newFileName)))
			{
				if (!savedAnswers[nameof(Command_File_Operations_RenameByExpression)].HasFlag(MessageOptions.All))
					savedAnswers[nameof(Command_File_Operations_RenameByExpression)] = new Message(state.TabsWindow)
					{
						Title = "Confirm",
						Text = "File already exists; overwrite?",
						Options = MessageOptions.YesNoAllCancel,
						DefaultCancel = MessageOptions.Cancel,
					}.Show();

				if (!savedAnswers[nameof(Command_File_Operations_RenameByExpression)].HasFlag(MessageOptions.Yes))
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

		void Command_File_Operations_Delete()
		{
			if (FileName == null)
				return;

			if (!savedAnswers[nameof(Command_File_Operations_Delete)].HasFlag(MessageOptions.All))
				savedAnswers[nameof(Command_File_Operations_Delete)] = new Message(state.TabsWindow)
				{
					Title = "Confirm",
					Text = "Are you sure you want to delete this file?",
					Options = MessageOptions.YesNoAll,
					DefaultAccept = MessageOptions.No,
					DefaultCancel = MessageOptions.No,
				}.Show();

			if (!savedAnswers[nameof(Command_File_Operations_Delete)].HasFlag(MessageOptions.Yes))
				return;

			File.Delete(FileName);
		}

		void Command_File_Operations_Explore() => Process.Start("explorer.exe", $"/select,\"{FileName}\"");

		void Command_File_Operations_CommandPrompt() => Process.Start(new ProcessStartInfo("cmd.exe") { WorkingDirectory = Path.GetDirectoryName(FileName) });

		void Command_File_Operations_VCSDiff()
		{
			//TODO
			//if (string.IsNullOrEmpty(FileName))
			//	throw new Exception("Must have filename to do diff");
			//var original = Versioner.GetUnmodifiedFile(FileName);
			//if (original == null)
			//	throw new Exception("Unable to get VCS content");

			//var textEdit = new TextEditor(displayName: Path.GetFileName(FileName), modified: false, bytes: original);
			//TabsParent.AddTextEditor(textEdit, index: TabsParent.GetTabIndex(this));
			//textEdit.ContentType = ContentType;
			//textEdit.DiffTarget = this;
		}

		void Command_File_Operations_SetDisplayName(GetExpressionDialog.Result result)
		{
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

		void Command_File_Close()
		{
			//TODO
			//if (CanClose())
			//	TabsParent.RemoveTextEditor(this);
		}

		public void Command_File_Refresh()
		{
			if (string.IsNullOrEmpty(FileName))
				return;

			if (!File.Exists(FileName))
				throw new Exception("This file has been deleted.");

			if (fileLastWrite != new FileInfo(FileName).LastWriteTime)
			{
				if (!savedAnswers[nameof(Command_File_Refresh)].HasFlag(MessageOptions.All))
					savedAnswers[nameof(Command_File_Refresh)] = new Message(state.TabsWindow)
					{
						Title = "Confirm",
						Text = "This file has been updated on disk. Reload?",
						Options = MessageOptions.YesNoAll,
						DefaultAccept = MessageOptions.Yes,
						DefaultCancel = MessageOptions.No,
					}.Show();

				if (!savedAnswers[nameof(Command_File_Refresh)].HasFlag(MessageOptions.Yes))
					return;

				Command_File_Revert();
			}
		}

		void Command_File_AutoRefresh(bool? multiStatus) => SetAutoRefresh(multiStatus != true);

		void Command_File_Revert()
		{
			if (IsModified)
			{
				if (!savedAnswers[nameof(Command_File_Revert)].HasFlag(MessageOptions.All))
					savedAnswers[nameof(Command_File_Revert)] = new Message(state.TabsWindow)
					{
						Title = "Confirm",
						Text = "You have unsaved changes. Are you sure you want to reload?",
						Options = MessageOptions.YesNoAllCancel,
						DefaultAccept = MessageOptions.No,
						DefaultCancel = MessageOptions.No,
					}.Show();
				if (!savedAnswers[nameof(Command_File_Revert)].HasFlag(MessageOptions.Yes))
					return;
			}

			var selections = Selections.ToList();
			var regions = Enumerable.Range(1, 9).ToDictionary(index => index, index => GetRegions(index));

			OpenFile(FileName, DisplayName, keepUndo: true);

			Func<IReadOnlyList<Range>, IReadOnlyList<Range>> reformatRanges = l => l.Select(range => new Range(Math.Max(0, Math.Min(range.Cursor, TextView.MaxPosition)), Math.Max(0, Math.Min(range.Anchor, TextView.MaxPosition)))).ToList();
			Selections = reformatRanges(selections);
			for (var region = 1; region <= 9; ++region)
				SetRegions(region, reformatRanges(GetRegions(region)));
		}

		void Command_File_Insert_Files()
		{
			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			var dialog = new OpenFileDialog { DefaultExt = "txt", Filter = "Text files|*.txt|All files|*.*", FilterIndex = 2, Multiselect = true };
			if (dialog.ShowDialog() == true)
				InsertFiles(dialog.FileNames);
		}

		void Command_File_Insert_CopiedCut()
		{
			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			var files = Clipboard;
			if (files.Count == 0)
				return;

			if (files.Count > 5)
			{
				if (!savedAnswers[nameof(Command_File_Insert_CopiedCut)].HasFlag(MessageOptions.All))
					savedAnswers[nameof(Command_File_Insert_CopiedCut)] = new Message(state.TabsWindow)
					{
						Title = "Confirm",
						Text = $"Are you sure you want to insert these {files.Count} files?",
						Options = MessageOptions.YesNoAllCancel,
						DefaultAccept = MessageOptions.Yes,
						DefaultCancel = MessageOptions.Cancel,
					}.Show();
				if (!savedAnswers[nameof(Command_File_Insert_CopiedCut)].HasFlag(MessageOptions.Yes))
					return;
			}

			InsertFiles(files);
		}

		void Command_File_Insert_Selected() => InsertFiles(RelativeSelectedFiles());

		EncodingDialog.Result Command_File_Encoding_Encoding_Dialog() => EncodingDialog.Run(state.TabsWindow, CodePage);

		void Command_File_Encoding_Encoding(EncodingDialog.Result result) => CodePage = result.CodePage;

		EncodingDialog.Result Command_File_Encoding_ReopenWithEncoding_Dialog() => EncodingDialog.Run(state.TabsWindow, CodePage);

		void Command_File_Encoding_ReopenWithEncoding(EncodingDialog.Result result)
		{
			if (IsModified)
			{
				if (!savedAnswers[nameof(Command_File_Encoding_ReopenWithEncoding)].HasFlag(MessageOptions.All))
					savedAnswers[nameof(Command_File_Encoding_ReopenWithEncoding)] = new Message(state.TabsWindow)
					{
						Title = "Confirm",
						Text = "You have unsaved changes. Are you sure you want to reload?",
						Options = MessageOptions.YesNoAll,
						DefaultAccept = MessageOptions.Yes,
						DefaultCancel = MessageOptions.No,
					}.Show();
				if (!savedAnswers[nameof(Command_File_Encoding_ReopenWithEncoding)].HasFlag(MessageOptions.Yes))
					return;
			}

			OpenFile(FileName, codePage: result.CodePage);
		}

		FileEncodingLineEndingsDialog.Result Command_File_Encoding_LineEndings_Dialog() => FileEncodingLineEndingsDialog.Run(state.TabsWindow, LineEnding ?? "");

		void Command_File_Encoding_LineEndings(FileEncodingLineEndingsDialog.Result result)
		{
			var lines = TextView.NumLines;
			var sel = new List<Range>();
			for (var line = 0; line < lines; ++line)
			{
				var current = Text.GetString(TextView.GetEnding(line));
				if ((current.Length == 0) || (current == result.LineEndings))
					continue;
				var start = TextView.GetPosition(line, TextView.GetLineLength(line));
				sel.Add(Range.FromIndex(start, current.Length));
			}
			Replace(sel, sel.Select(str => result.LineEndings).ToList());
		}

		string Command_File_Encrypt_Dialog(bool? multiStatus)
		{
			if (multiStatus != false)
				return "";

			return FileSaver.GetKey(state.TabsWindow, true);
		}

		void Command_File_Encrypt(string result) => AESKey = result == "" ? null : result;

		void Command_File_Compress(bool? multiStatus) => Compressed = multiStatus == false;
	}
}
