using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Win32;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Transform;
using NeoEdit.Editor.TaskRunning;

namespace NeoEdit.Editor
{
	partial class Tabs
	{
		public void OpenFiles(IReadOnlyList<string> files) => files.AsTaskRunner().Select(file => new Tab(file)).SequentialForEach(tab => AddTab(tab));

		void Execute_File_New_New(bool createTabs)
		{
			var tabs = this;
			if (createTabs)
				tabs = new Tabs();
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

		Configuration_File_Open_Open Configure_File_Open_Open(string initialDirectory = null)
		{
			if ((initialDirectory == null) && (Focused != null))
				initialDirectory = Path.GetDirectoryName(Focused.FileName);
			var result = TabsWindow.Configure_File_Open_Open("txt", initialDirectory, "Text files|*.txt|All files|*.*", 2, true);
			if (result == null)
				throw new OperationCanceledException();
			return result;
		}

		void Execute_File_Open_Open(Configuration_File_Open_Open result) => result.FileNames.ForEach(fileName => AddTab(new Tab(fileName)));

		void Execute_File_Open_CopiedCut() => OpenFiles(NEClipboard.Current.Strings);

		void Execute_File_MoveToNewWindow()
		{
			var active = ActiveTabs.ToList();
			active.ForEach(tab => RemoveTab(tab));

			var tabs = new Tabs();
			tabs.SetLayout(WindowLayout);
			active.ForEach(tab => tabs.AddTab(tab));
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
				AddToTransaction(tab);
				tab.VerifyCanClose();
				RemoveTab(tab);
			}
			Instances.Remove(this);
			TabsWindow.CloseWindow();

			if (!Instances.Any())
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
		}
	}
}
