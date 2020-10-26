using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Win32;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Transform;
using NeoEdit.Editor.PreExecution;
using NeoEdit.TaskRunning;

namespace NeoEdit.Editor
{
	partial class Tab
	{
		string GetSaveFileName()
		{
			return Tabs.ShowTab(this, () =>
			{
				var result = Tabs.TabsWindow.RunSaveFileDialog(Path.GetFileName(FileName) ?? DisplayName, "txt", Path.GetDirectoryName(FileName), "All files|*.*");
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

		static PreExecutionStop PreExecute_File_New_New(EditorExecuteState state)
		{
			state.Tabs.AddTab(new Tab(), canReplace: false);
			return PreExecutionStop.Stop;
		}

		void Execute_File_New_FromSelections() => GetSelectionStrings().ForEach(((str, index) => QueueAddTab(new Tab(displayName: $"Selection {index + 1}", bytes: Coder.StringToBytes(str, Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, contentType: ContentType, modified: false))));

		static PreExecutionStop PreExecute_File_New_FromClipboards(EditorExecuteState state)
		{
			Tabs.AddTabsFromClipboards(state.Tabs);
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_File_New_FromClipboardSelections(EditorExecuteState state)
		{
			Tabs.AddTabsFromClipboardSelections(state.Tabs);
			return PreExecutionStop.Stop;
		}

		static Configuration_File_Open_Open Configure_File_Open_Open(EditorExecuteState state, string initialDirectory = null)
		{
			if ((initialDirectory == null) && (state.Tabs.Focused != null))
				initialDirectory = Path.GetDirectoryName(state.Tabs.Focused.FileName);
			var result = state.Tabs.TabsWindow.Configure_File_Open_Open("txt", initialDirectory, "Text files|*.txt|All files|*.*", 2, true);
			if (result == null)
				throw new OperationCanceledException();
			return result;
		}

		static PreExecutionStop PreExecute_File_Open_Open(EditorExecuteState state)
		{
			var result = state.Configuration as Configuration_File_Open_Open;
			result.FileNames.ForEach(fileName => state.Tabs.AddTab(new Tab(fileName)));
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_File_Open_CopiedCut(EditorExecuteState state)
		{
			NEClipboard.Current.Strings.AsTaskRunner().Select(file => new Tab(file)).ForEach(tab => state.Tabs.AddTab(tab));
			return PreExecutionStop.Stop;
		}

		void Execute_File_Open_Selected() => RelativeSelectedFiles().AsTaskRunner().Select(file => new Tab(file)).ForEach(tab => state.Tabs.AddTab(tab));

		void Execute_File_Save_Save()
		{
			if (FileName == null)
				Execute_File_SaveCopy_SaveCopy();
			else
				Save(FileName);
		}

		void Execute_File_Save_SaveModified()
		{
			if ((FileName != null) && (!IsModified))
				return;
			Execute_File_Save_Save();
		}

		void Execute_File_SaveCopy_SaveCopy(bool copyOnly = false) => Save(GetSaveFileName(), copyOnly);

		static Configuration_File_SaveCopyRename_ByExpression Configure_File_SaveCopy_SaveCopyByExpression(EditorExecuteState state) => state.Tabs.TabsWindow.Configure_File_SaveCopyRename_ByExpression(state.Tabs.Focused.GetVariables(), state.Tabs.Focused.Selections.Count);

		void Execute_File_SaveCopy_SaveCopyByExpression(bool copyOnly = false)
		{
			var result = state.Configuration as Configuration_File_SaveCopyRename_ByExpression;
			var results = GetExpressionResults<string>(result.Expression, Selections.Count());
			if (results.Count != 1)
				throw new Exception("Only one filename may be specified");

			var newFileName = FileName.RelativeChild(results[0]);

			if (File.Exists(newFileName))
			{
				if (!QueryUser(nameof(Execute_File_SaveCopy_SaveCopyByExpression), "File already exists; overwrite?", MessageOptions.None))
					return;
			}

			Save(newFileName, copyOnly);
		}

		void Execute_File_Copy_Path() => ClipboardCopy = new List<string> { FileName };

		void Execute_File_Copy_Name() => Clipboard = new List<string> { Path.GetFileName(FileName) };

		void Execute_File_Copy_DisplayName() => Clipboard = new List<string> { DisplayName ?? Path.GetFileName(FileName) };

		void Execute_File_Rename_Rename()
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

		static Configuration_File_SaveCopyRename_ByExpression Configure_File_Rename_RenameByExpression(EditorExecuteState state) => state.Tabs.TabsWindow.Configure_File_SaveCopyRename_ByExpression(state.Tabs.Focused.GetVariables(), state.Tabs.Focused.Selections.Count);

		void Execute_File_Rename_RenameByExpression()
		{
			var result = state.Configuration as Configuration_File_SaveCopyRename_ByExpression;
			var results = GetExpressionResults<string>(result.Expression, Selections.Count());
			if (results.Count != 1)
				throw new Exception("Only one filename may be specified");

			var newFileName = FileName.RelativeChild(results[0]);

			if ((!string.Equals(newFileName, FileName, StringComparison.OrdinalIgnoreCase)) && (File.Exists(newFileName)))
			{
				if (!QueryUser(nameof(Execute_File_Rename_RenameByExpression), "File already exists; overwrite?", MessageOptions.None))
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

			if (!QueryUser(nameof(Execute_File_Operations_Delete), "Are you sure you want to delete this file?", MessageOptions.No))
				return;

			File.Delete(FileName);
		}

		void Execute_File_Operations_Explore() => Process.Start("explorer.exe", $"/select,\"{FileName}\"");

		void Execute_File_Operations_CommandPrompt() => Process.Start(new ProcessStartInfo("cmd.exe") { WorkingDirectory = Path.GetDirectoryName(FileName) });

		void Execute_File_Operations_DragDrop() => newDragFiles.Add(FileName);

		void Execute_File_Operations_VCSDiff()
		{
			if (string.IsNullOrEmpty(FileName))
				throw new Exception("Must have filename to do diff");
			var original = Versioner.GetUnmodifiedFile(FileName);
			if (original == null)
				throw new Exception("Unable to get VCS content");

			var tab = new Tab(displayName: Path.GetFileName(FileName), modified: false, bytes: original);
			Tabs.AddToTransaction(tab);
			tab.ContentType = ContentType;
			tab.DiffTarget = this;
			QueueAddTab(tab, Tabs.GetTabIndex(this));
		}

		void Execute_File_Operations_SetDisplayName()
		{
			var result = state.Configuration as Configuration_File_SaveCopyRename_ByExpression;
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
			VerifyCanClose();
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

		static PreExecutionStop PreExecute_File_MoveToNewWindow(EditorExecuteState state)
		{
			var active = state.Tabs.ActiveTabs.ToList();
			active.ForEach(tab => state.Tabs.RemoveTab(tab));

			var tabs = new Tabs();
			tabs.SetLayout(state.Tabs.WindowLayout);
			active.ForEach(tab => tabs.AddTab(tab));

			return PreExecutionStop.Stop;
		}

		void Execute_File_Insert_Files()
		{
			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			var result = Tabs.TabsWindow.Configure_File_Open_Open("txt", filter: "Text files|*.txt|All files|*.*", filterIndex: 2, multiselect: true);
			if (result != null)
				InsertFiles(result.FileNames);
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
				if (!QueryUser(nameof(Execute_File_Insert_CopiedCut), $"Are you sure you want to insert these {files.Count} files?", MessageOptions.Yes))
					return;
			}

			InsertFiles(files);
		}

		void Execute_File_Insert_Selected() => InsertFiles(RelativeSelectedFiles());

		static Configuration_File_Encoding_Encoding Configure_File_Encoding_Encoding(EditorExecuteState state) => state.Tabs.TabsWindow.Configure_File_Encoding_Encoding(state.Tabs.Focused.CodePage);

		void Execute_File_Encoding_Encoding()
		{
			var result = state.Configuration as Configuration_File_Encoding_Encoding;
			CodePage = result.CodePage;
		}

		static Configuration_File_Encoding_Encoding Configure_File_Encoding_ReopenWithEncoding(EditorExecuteState state) => state.Tabs.TabsWindow.Configure_File_Encoding_Encoding(state.Tabs.Focused.CodePage);

		void Execute_File_Encoding_ReopenWithEncoding()
		{
			var result = state.Configuration as Configuration_File_Encoding_Encoding;
			if (IsModified)
			{
				if (!QueryUser(nameof(Execute_File_Encoding_ReopenWithEncoding), "You have unsaved changes. Are you sure you want to reload?", MessageOptions.Yes))
					return;
			}

			OpenFile(FileName, codePage: result.CodePage);
		}

		static Configuration_File_Encoding_LineEndings Configure_File_Encoding_LineEndings(EditorExecuteState state)
		{
			var endings = state.Tabs.ActiveTabs.Select(tab => tab.Text.OnlyEnding).Distinct().Take(2).ToList();
			var ending = endings.Count == 1 ? endings[0] : "";
			return state.Tabs.TabsWindow.Configure_File_Encoding_LineEndings(ending);
		}

		void Execute_File_Encoding_LineEndings()
		{
			var result = state.Configuration as Configuration_File_Encoding_LineEndings;
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

		static Configuration_File_Encrypt Configure_File_Encrypt(EditorExecuteState state)
		{
			if (state.MultiStatus != false)
				return new Configuration_File_Encrypt();
			else
				return state.Tabs.TabsWindow.Configure_File_Encrypt(Cryptor.Type.AES, true);
		}

		void Execute_File_Encrypt() => AESKey = (state.Configuration as Configuration_File_Encrypt).Key;

		void Execute_File_Compress() => Compressed = state.MultiStatus == false;

		static PreExecutionStop PreExecute_File_Shell_Integrate(EditorExecuteState state)
		{
			using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Default))
			using (var starKey = baseKey.OpenSubKey("*"))
			using (var shellKey = starKey.OpenSubKey("shell", true))
			using (var neoEditKey = shellKey.CreateSubKey("Open with NeoEdit"))
			using (var commandKey = neoEditKey.CreateSubKey("command"))
				commandKey.SetValue("", $@"""{Assembly.GetEntryAssembly().Location}"" -text ""%1""");

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_File_Shell_Unintegrate(EditorExecuteState state)
		{
			using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Default))
			using (var starKey = baseKey.OpenSubKey("*"))
			using (var shellKey = starKey.OpenSubKey("shell", true))
				shellKey.DeleteSubKeyTree("Open with NeoEdit");

			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_File_DontExitOnClose(EditorExecuteState state)
		{
			Settings.DontExitOnClose = state.MultiStatus != true;
			return PreExecutionStop.Stop;
		}

		static PreExecutionStop PreExecute_File_Exit(EditorExecuteState state)
		{
			foreach (var tab in state.Tabs.AllTabs)
			{
				state.Tabs.AddToTransaction(tab);
				tab.VerifyCanClose();
				state.Tabs.RemoveTab(tab);
			}
			Tabs.Instances.Remove(state.Tabs);
			state.Tabs.TabsWindow.CloseWindow();

			if (!Tabs.Instances.Any())
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
