using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Win32;
using NeoEdit.Common;
using NeoEdit.Common.Dialogs;
using NeoEdit.Common.NEClipboards;
using NeoEdit.Common.Transform;

namespace NeoEdit
{
	partial class Tabs
	{
		void Command_File_New_New(bool createTabs) => (createTabs ? new Tabs() : this).Add();

		void Command_File_New_FromClipboards()
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
				var te = Add(displayName: $"Clipboard {index}", bytes: Coder.StringToBytes(sb.ToString(), Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, modified: false);
				te.SetSelections(sels);
			}
		}

		void Command_File_New_FromClipboardSelections() => NEClipboard.Current.Strings.ForEach((str, index) => Add(displayName: $"Clipboard {index + 1}", bytes: Coder.StringToBytes(str, Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, modified: false));

		OpenFileDialogResult Command_File_Open_Open_Dialog(string initialDirectory = null)
		{
			if ((initialDirectory == null) && (TopMost != null))
				initialDirectory = Path.GetDirectoryName(TopMost.FileName);
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

		void Command_File_Open_Open(OpenFileDialogResult result) => result.files.ForEach(fileName => Add(fileName));

		void Command_File_Open_CopiedCut()
		{
			var files = NEClipboard.Current.Strings;

			if ((files.Count > 5) && (new Message(WindowParent)
			{
				Title = "Confirm",
				Text = $"Are you sure you want to open these {files.Count} files?",
				Options = MessageOptions.YesNoCancel,
				DefaultAccept = MessageOptions.Yes,
				DefaultCancel = MessageOptions.Cancel,
			}.Show() != MessageOptions.Yes))
				return;

			foreach (var item in Items)
				item.Active = false;
			foreach (var file in files)
				Add(file);
		}

		void Command_File_Operations_DragDrop()
		{
			if (TopMost == null)
				throw new Exception("No active file");
			var fileNames = Items.Where(te => te.Active).Select(te => te.FileName).NonNullOrEmpty().ToList();
			if (!fileNames.Any())
				throw new Exception("No current files have filenames.");
			var nonExisting = fileNames.Where(x => !File.Exists(x));
			if (nonExisting.Any())
				throw new Exception($"The following files don't exist:\n\n{string.Join("\n", nonExisting)}");
			TopMost.DragFiles = fileNames;
		}

		void Command_File_MoveToNewWindow()
		{
			var active = Items.Where(tab => tab.Active).ToList();
			active.ForEach(tab => Items.Remove(tab));

			var newWindow = new Tabs();
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
