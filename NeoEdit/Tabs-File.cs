using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Win32;
using NeoEdit.Program.Dialogs;
using NeoEdit.Program.NEClipboards;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program
{
	partial class Tabs
	{
		public void OpenFiles(IEnumerable<string> files)
		{
			RunTasksDialog.AddTasks(files, (file, progress) =>
			{
				progress.Name = Path.GetFileName(file);
				return new Tab(file);
			}, results => results.ForEach(tab => AddTab(tab)));
		}

		void Execute_File_New_New(bool createTabs)
		{
			var tabs = this;
			if (createTabs)
			{
				var tabsWindow = new TabsWindow();
				tabs = tabsWindow.Tabs;
			}
			tabs.AddTab(new Tab(), canReplace: false);
		}

		void Execute_File_New_FromClipboards()
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
				var te = new Tab(displayName: $"Clipboard {index}", bytes: Coder.StringToBytes(sb.ToString(), Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, modified: false);
				AddTab(te, canReplace: index == 1);
				te.Selections = sels;
			}
		}

		void Execute_File_New_FromClipboardSelections() => NEClipboard.Current.Strings.ForEach((str, index) => AddTab(new Tab(displayName: $"Clipboard {index + 1}", bytes: Coder.StringToBytes(str, Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, modified: false)));

		OpenFileDialogResult Configure_File_Open_Open(string initialDirectory = null)
		{
			if ((initialDirectory == null) && (Focused != null))
				initialDirectory = Path.GetDirectoryName(Focused.FileName);
			var dialog = new OpenFileDialog
			{
				DefaultExt = "txt",
				Filter = "Text files|*.txt|All files|*.*",
				FilterIndex = 2,
				Multiselect = true,
				InitialDirectory = initialDirectory,
			};
			if (dialog.ShowDialog() != true)
				throw new OperationCanceledException();

			return new OpenFileDialogResult { files = dialog.FileNames.ToList() };
		}

		void Execute_File_Open_Open(OpenFileDialogResult result) => result.files.ForEach(fileName => AddTab(new Tab(fileName)));

		void Execute_File_Open_CopiedCut()
		{
			var files = NEClipboard.Current.Strings;

			if ((files.Count > 5) && (!state.TabsWindow.RunMessageDialog("Confirm", $"Are you sure you want to open these {files.Count} files?", MessageOptions.YesNoCancel, MessageOptions.Yes, MessageOptions.Cancel).HasFlag(MessageOptions.Yes)))
				return;

			OpenFiles(files);
		}

		void Execute_File_MoveToNewWindow()
		{
			var active = SortedActiveTabs.ToList();
			active.ForEach(tab => RemoveTab(tab));

			var tabsWindow = new TabsWindow();
			tabsWindow.Tabs.SetLayout(Columns, Rows, MaxColumns, MaxRows);
			active.ForEach(tab => tabsWindow.Tabs.AddTab(tab));
		}

		static void Execute_File_Shell_Integrate()
		{
			using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Default))
			using (var starKey = baseKey.OpenSubKey("*"))
			using (var shellKey = starKey.OpenSubKey("shell", true))
			using (var neoEditKey = shellKey.CreateSubKey("Open with NeoEdit"))
			using (var commandKey = neoEditKey.CreateSubKey("command"))
				commandKey.SetValue("", $@"""{Assembly.GetEntryAssembly().Location}"" -text ""%1""");
		}

		static void Execute_File_Shell_Unintegrate()
		{
			using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Default))
			using (var starKey = baseKey.OpenSubKey("*"))
			using (var shellKey = starKey.OpenSubKey("shell", true))
				shellKey.DeleteSubKeyTree("Open with NeoEdit");
		}

		void Execute_File_DontExitOnClose(bool? multiStatus) => Settings.DontExitOnClose = multiStatus != true;

		void Execute_File_Exit()
		{
			foreach (var tab in AllTabs)
			{
				tab.VerifyCanClose();
				RemoveTab(tab);
			}
		}
	}
}
