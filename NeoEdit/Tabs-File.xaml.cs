using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Win32;
using NeoEdit.Common;
using NeoEdit.Common.Controls;
using NeoEdit.Common.Dialogs;
using NeoEdit.Common.NEClipboards;
using NeoEdit.Common.Transform;
using NeoEdit.Dialogs;

namespace NeoEdit
{
	partial class Tabs
	{
		static public void Command_File_New_New(ITabs tabs, bool createTabs) => (createTabs ? ITabsCreator.CreateTabs() : tabs).Add();

		static public void Command_File_New_FromClipboards(ITabs tabs)
		{
			var index = 0;
			foreach (var clipboard in NEClipboard.Current)
			{
				++index;
				var strs = clipboard.Strings;
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
				var te = tabs.Add(displayName: $"Clipboard {index}", bytes: Coder.StringToBytes(sb.ToString(), Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, modified: false);
				te.SetSelections(sels);
			}
		}

		static public void Command_File_New_FromClipboardSelections(ITabs tabs) => NEClipboard.Current.Strings.ForEach((str, index) => tabs.Add(displayName: $"Clipboard {index + 1}", bytes: Coder.StringToBytes(str, Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, modified: false));

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

		static public void Command_File_Operations_DragDrop(ITabs tabs)
		{
			if (tabs.TopMost == null)
				throw new Exception("No active file");
			var fileNames = tabs.Items.Where(te => te.Active).Select(te => te.FileName).NonNullOrEmpty().ToList();
			if (!fileNames.Any())
				throw new Exception("No current files have filenames.");
			var nonExisting = fileNames.Where(x => !File.Exists(x));
			if (nonExisting.Any())
				throw new Exception($"The following files don't exist:\n\n{string.Join("\n", nonExisting)}");
			tabs.TopMost.DragFiles = fileNames;
		}

		static public void Command_File_MoveToNewWindow(ITabs tabs)
		{
			var active = tabs.Items.Where(tab => tab.Active).ToList();
			active.ForEach(tab => tabs.Items.Remove(tab));

			var newWindow = ITabsCreator.CreateTabs();
			newWindow.SetLayout(newWindow.Layout, newWindow.Columns, newWindow.Rows);
			active.ForEach(tab => newWindow.Add(tab));
		}

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
