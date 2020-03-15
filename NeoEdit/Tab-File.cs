using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using NeoEdit.Program.Dialogs;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program
{
	partial class Tab
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

		void Execute_File_New_FromSelections() => GetSelectionStrings().ForEach(((str, index) => Tabs.AddTab(new Tab(displayName: $"Selection {index + 1}", bytes: Coder.StringToBytes(str, Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, contentType: ContentType, modified: false))));

		void Execute_File_Open_Selected()
		{
			var files = RelativeSelectedFiles();
			foreach (var file in files)
				Tabs.AddTab(new Tab(file));
		}

		void Execute_File_Save_Save()
		{
			if (FileName == null)
				Execute_File_SaveCopy_SaveCopy();
			else
				Save(FileName);
		}

		void Execute_File_SaveCopy_SaveCopy(bool copyOnly = false) => Save(GetSaveFileName(), copyOnly);

		object Configure_File_SaveCopy_SaveCopyByExpression() => GetExpressionDialog.Run(state.TabsWindow, GetVariables(), Selections.Count);

		void Execute_File_SaveCopy_SaveCopyClipboard(bool copyOnly = false)
		{
			var results = Clipboard;
			if (results.Count != 1)
				throw new Exception("Only one filename may be specified");

			var newFileName = FileName.RelativeChild(results[0]);

			if (File.Exists(newFileName))
			{
				if (!state.SavedAnswers[nameof(Execute_File_SaveCopy_SaveCopyClipboard)].HasFlag(MessageOptions.All))
					state.SavedAnswers[nameof(Execute_File_SaveCopy_SaveCopyClipboard)] = new Message(state.TabsWindow)
					{
						Title = "Confirm",
						Text = "File already exists; overwrite?",
						Options = MessageOptions.YesNoAllCancel,
						DefaultCancel = MessageOptions.Cancel,
					}.Show();

				if (!state.SavedAnswers[nameof(Execute_File_SaveCopy_SaveCopyClipboard)].HasFlag(MessageOptions.Yes))
					return;
			}

			Save(newFileName, copyOnly);
		}

		void Execute_File_SaveCopy_SaveCopyByExpression(bool copyOnly = false)
		{
			var result = state.Configuration as GetExpressionDialog.Result;
			var results = GetExpressionResults<string>(result.Expression, Selections.Count());
			if (results.Count != 1)
				throw new Exception("Only one filename may be specified");

			var newFileName = FileName.RelativeChild(results[0]);

			if (File.Exists(newFileName))
			{
				if (!state.SavedAnswers[nameof(Execute_File_SaveCopy_SaveCopyByExpression)].HasFlag(MessageOptions.All))
					state.SavedAnswers[nameof(Execute_File_SaveCopy_SaveCopyByExpression)] = new Message(state.TabsWindow)
					{
						Title = "Confirm",
						Text = "File already exists; overwrite?",
						Options = MessageOptions.YesNoAllCancel,
						DefaultCancel = MessageOptions.Cancel,
					}.Show();

				if (!state.SavedAnswers[nameof(Execute_File_SaveCopy_SaveCopyByExpression)].HasFlag(MessageOptions.Yes))
					return;
			}

			Save(newFileName, copyOnly);
		}

		void Execute_File_Copy_Path() => ClipboardCopy = new List<string> { FileName };

		void Execute_File_Copy_Name() => Clipboard = new List<string> { Path.GetFileName(FileName) };

		void Execute_File_Copy_DisplayName() => Clipboard = new List<string> { DisplayName ?? Path.GetFileName(FileName) };

		void Execute_File_Operations_Rename()
		{
			if (string.IsNullOrEmpty(FileName))
			{
				Execute_File_SaveCopy_SaveCopy();
				return;
			}

			var fileName = GetSaveFileName();

			if (!string.Equals(FileName, fileName, StringComparison.OrdinalIgnoreCase))
				File.Delete(fileName);
			File.Move(FileName, fileName);
			SetFileName(fileName);
		}

		object Configure_File_Operations_RenameByExpression() => GetExpressionDialog.Run(state.TabsWindow, GetVariables(), Selections.Count);

		void Execute_File_Operations_RenameClipboard()
		{
			var results = Clipboard;
			if (results.Count != 1)
				throw new Exception("Only one filename may be specified");

			var newFileName = FileName.RelativeChild(results[0]);

			if ((!string.Equals(newFileName, FileName, StringComparison.OrdinalIgnoreCase)) && (File.Exists(newFileName)))
			{
				if (!state.SavedAnswers[nameof(Execute_File_Operations_RenameClipboard)].HasFlag(MessageOptions.All))
					state.SavedAnswers[nameof(Execute_File_Operations_RenameClipboard)] = new Message(state.TabsWindow)
					{
						Title = "Confirm",
						Text = "File already exists; overwrite?",
						Options = MessageOptions.YesNoAllCancel,
						DefaultCancel = MessageOptions.Cancel,
					}.Show();

				if (!state.SavedAnswers[nameof(Execute_File_Operations_RenameClipboard)].HasFlag(MessageOptions.Yes))
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

		void Execute_File_Operations_RenameByExpression()
		{
			var result = state.Configuration as GetExpressionDialog.Result;
			var results = GetExpressionResults<string>(result.Expression, Selections.Count());
			if (results.Count != 1)
				throw new Exception("Only one filename may be specified");

			var newFileName = FileName.RelativeChild(results[0]);

			if ((!string.Equals(newFileName, FileName, StringComparison.OrdinalIgnoreCase)) && (File.Exists(newFileName)))
			{
				if (!state.SavedAnswers[nameof(Execute_File_Operations_RenameByExpression)].HasFlag(MessageOptions.All))
					state.SavedAnswers[nameof(Execute_File_Operations_RenameByExpression)] = new Message(state.TabsWindow)
					{
						Title = "Confirm",
						Text = "File already exists; overwrite?",
						Options = MessageOptions.YesNoAllCancel,
						DefaultCancel = MessageOptions.Cancel,
					}.Show();

				if (!state.SavedAnswers[nameof(Execute_File_Operations_RenameByExpression)].HasFlag(MessageOptions.Yes))
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

		void Execute_File_Operations_Delete()
		{
			if (FileName == null)
				return;

			if (!state.SavedAnswers[nameof(Execute_File_Operations_Delete)].HasFlag(MessageOptions.All))
				state.SavedAnswers[nameof(Execute_File_Operations_Delete)] = new Message(state.TabsWindow)
				{
					Title = "Confirm",
					Text = "Are you sure you want to delete this file?",
					Options = MessageOptions.YesNoAll,
					DefaultAccept = MessageOptions.No,
					DefaultCancel = MessageOptions.No,
				}.Show();

			if (!state.SavedAnswers[nameof(Execute_File_Operations_Delete)].HasFlag(MessageOptions.Yes))
				return;

			File.Delete(FileName);
		}

		void Execute_File_Operations_Explore() => Process.Start("explorer.exe", $"/select,\"{FileName}\"");

		void Execute_File_Operations_CommandPrompt() => Process.Start(new ProcessStartInfo("cmd.exe") { WorkingDirectory = Path.GetDirectoryName(FileName) });

		void Execute_File_Operations_DragDrop() => ChangedDragFiles = new List<string> { FileName };

		void Execute_File_Operations_VCSDiff()
		{
			//TODO
			//if (string.IsNullOrEmpty(FileName))
			//	throw new Exception("Must have filename to do diff");
			//var original = Versioner.GetUnmodifiedFile(FileName);
			//if (original == null)
			//	throw new Exception("Unable to get VCS content");

			//var tab = new Tab(displayName: Path.GetFileName(FileName), modified: false, bytes: original);
			//TabsParent.AddTab(tab, index: TabsParent.GetTabIndex(this));
			//tab.ContentType = ContentType;
			//tab.DiffTarget = this;
		}

		void Execute_File_Operations_SetDisplayName()
		{
			var result = state.Configuration as GetExpressionDialog.Result;
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

		void Execute_File_Close()
		{
			if (CanClose())
				Tabs.RemoveTab(this);
		}

		void Execute_File_Refresh()
		{
			if (string.IsNullOrEmpty(FileName))
				return;

			if (!File.Exists(FileName))
				throw new Exception("This file has been deleted.");

			if (fileLastWrite != new FileInfo(FileName).LastWriteTime)
			{
				if (!state.SavedAnswers[nameof(Execute_File_Refresh)].HasFlag(MessageOptions.All))
					state.SavedAnswers[nameof(Execute_File_Refresh)] = new Message(state.TabsWindow)
					{
						Title = "Confirm",
						Text = "This file has been updated on disk. Reload?",
						Options = MessageOptions.YesNoAll,
						DefaultAccept = MessageOptions.Yes,
						DefaultCancel = MessageOptions.No,
					}.Show();

				if (!state.SavedAnswers[nameof(Execute_File_Refresh)].HasFlag(MessageOptions.Yes))
					return;

				Execute_File_Revert();
			}
		}

		void Execute_File_AutoRefresh() => SetAutoRefresh(state.MultiStatus != true);

		void Execute_File_Revert()
		{
			if (IsModified)
			{
				if (!state.SavedAnswers[nameof(Execute_File_Revert)].HasFlag(MessageOptions.All))
					state.SavedAnswers[nameof(Execute_File_Revert)] = new Message(state.TabsWindow)
					{
						Title = "Confirm",
						Text = "You have unsaved changes. Are you sure you want to reload?",
						Options = MessageOptions.YesNoAllCancel,
						DefaultAccept = MessageOptions.No,
						DefaultCancel = MessageOptions.No,
					}.Show();
				if (!state.SavedAnswers[nameof(Execute_File_Revert)].HasFlag(MessageOptions.Yes))
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

		void Execute_File_Insert_Files()
		{
			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			var dialog = new OpenFileDialog { DefaultExt = "txt", Filter = "Text files|*.txt|All files|*.*", FilterIndex = 2, Multiselect = true };
			if (dialog.ShowDialog() == true)
				InsertFiles(dialog.FileNames);
		}

		void Execute_File_Insert_CopiedCut()
		{
			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			var files = Clipboard;
			if (files.Count == 0)
				return;

			if (files.Count > 5)
			{
				if (!state.SavedAnswers[nameof(Execute_File_Insert_CopiedCut)].HasFlag(MessageOptions.All))
					state.SavedAnswers[nameof(Execute_File_Insert_CopiedCut)] = new Message(state.TabsWindow)
					{
						Title = "Confirm",
						Text = $"Are you sure you want to insert these {files.Count} files?",
						Options = MessageOptions.YesNoAllCancel,
						DefaultAccept = MessageOptions.Yes,
						DefaultCancel = MessageOptions.Cancel,
					}.Show();
				if (!state.SavedAnswers[nameof(Execute_File_Insert_CopiedCut)].HasFlag(MessageOptions.Yes))
					return;
			}

			InsertFiles(files);
		}

		void Execute_File_Insert_Selected() => InsertFiles(RelativeSelectedFiles());

		object Configure_File_Encoding_Encoding() => EncodingDialog.Run(state.TabsWindow, CodePage);

		void Execute_File_Encoding_Encoding()
		{
			var result = state.Configuration as EncodingDialog.Result;
			CodePage = result.CodePage;
		}

		object Configure_File_Encoding_ReopenWithEncoding() => EncodingDialog.Run(state.TabsWindow, CodePage);

		void Execute_File_Encoding_ReopenWithEncoding()
		{
			var result = state.Configuration as EncodingDialog.Result;
			if (IsModified)
			{
				if (!state.SavedAnswers[nameof(Execute_File_Encoding_ReopenWithEncoding)].HasFlag(MessageOptions.All))
					state.SavedAnswers[nameof(Execute_File_Encoding_ReopenWithEncoding)] = new Message(state.TabsWindow)
					{
						Title = "Confirm",
						Text = "You have unsaved changes. Are you sure you want to reload?",
						Options = MessageOptions.YesNoAll,
						DefaultAccept = MessageOptions.Yes,
						DefaultCancel = MessageOptions.No,
					}.Show();
				if (!state.SavedAnswers[nameof(Execute_File_Encoding_ReopenWithEncoding)].HasFlag(MessageOptions.Yes))
					return;
			}

			OpenFile(FileName, codePage: result.CodePage);
		}

		object Configure_File_Encoding_LineEndings() => FileEncodingLineEndingsDialog.Run(state.TabsWindow, LineEnding ?? "");

		void Execute_File_Encoding_LineEndings()
		{
			var result = state.Configuration as FileEncodingLineEndingsDialog.Result;
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

		object Configure_File_Encrypt()
		{
			if (state.MultiStatus != false)
				return "";
			else
				return FileSaver.GetKey(state.TabsWindow, true);
		}

		void Execute_File_Encrypt()
		{
			var result = state.Configuration as string;
			AESKey = result == "" ? null : result;
		}

		void Execute_File_Compress() => Compressed = state.MultiStatus == false;
	}
}
