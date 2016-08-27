using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using NeoEdit.Disk;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;
using NeoEdit.Handles;
using NeoEdit.HexEdit;
using NeoEdit.ImageEdit;
using NeoEdit.Network;
using NeoEdit.Processes;
using NeoEdit.TextEdit;
using NeoEdit.TextView;

namespace NeoEdit
{
	partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			EventManager.RegisterClassHandler(typeof(TextBox), TextBox.GotFocusEvent, new RoutedEventHandler((s, e2) => (s as TextBox).SelectAll()));
			base.OnStartup(e);
			// Without the ShutdownMode lines, the program will close if a dialog is displayed and closed before any windows
			ShutdownMode = ShutdownMode.OnExplicitShutdown;
			CreateWindowsFromArgs(string.Join(" ", e.Args.Select(str => $"\"{str}\"")));
			ShutdownMode = ShutdownMode.OnLastWindowClose;
			if (Application.Current.Windows.Count == 0)
				Application.Current.Shutdown();
		}

		void ShowExceptionMessage(Exception ex)
		{
			var message = "";
			for (var ex2 = ex; ex2 != null; ex2 = ex2.InnerException)
				message += $"{ex2.Message}\n";
			Message.Show(message, "Error");
#if DEBUG
			if ((Debugger.IsAttached) && ((Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None))
			{
				var inner = ex;
				while (inner.InnerException != null)
					inner = inner.InnerException;
				var er = inner?.StackTrace?.Split('\r', '\n').FirstOrDefault(a => a.Contains(":line"));
				if (er != null)
				{
					var idx = er.LastIndexOf(" in ");
					if (idx != -1)
						er = er.Substring(idx + 4);
					idx = er.IndexOf(":line ");
					er = $"{er.Substring(0, idx)} {er.Substring(idx + 6)}";
					NoDelayClipboard.SetText(er);
				}
				Debugger.Break();
			}
#endif

		}

		public void CreateWindowsFromArgs(string commandLine)
		{
			try
			{
				var paramList = CommandLineParams.CommandLineVisitor.GetCommandLineParams(commandLine);

				var windows = UIHelper<NEWindow>.GetAllWindows();
				var restored = windows.Count(window => window.Restore());
				if ((restored > 0) && (!paramList.Any()))
					return;

				if (!paramList.Any())
					TextEditTabs.Create();
				foreach (var param in paramList)
					param.Execute();
			}
			catch (Exception ex) { ShowExceptionMessage(ex); }
		}

		public App()
		{
			NeoEdit.GUI.Launcher.Initialize(
				getEscapeClearsSelections: () => NeoEdit.Properties.Settings.Default.EscapeClearsSelections
				, setEscapeClearsSelections: value => { NeoEdit.Properties.Settings.Default.EscapeClearsSelections = value; NeoEdit.Properties.Settings.Default.Save(); }
				, getMinimizeToTray: () => NeoEdit.Properties.Settings.Default.MinimizeToTray
				, setMinimizeToTray: value => { NeoEdit.Properties.Settings.Default.MinimizeToTray = value; NeoEdit.Properties.Settings.Default.Save(); }

				, diff: () => TextEditTabs.CreateDiff().AddDiff()
				, disk: (path, files, forceCreate) => DiskTabs.Create(path, files, forceCreate: forceCreate)
				, fileHexEditor: (fileName, binarydata, encoder, modified, forceCreate) => HexEditTabs.CreateFromFile(fileName, binarydata, encoder, modified, forceCreate)
				, handles: (pid) => new HandlesWindow(pid)
				, imageEditor: fileName => ImageEditTabs.Create(fileName)
				, network: () => new NetworkWindow()
				, processes: (pid) => new ProcessesWindow(pid)
				, processHexEditor: (pid) => HexEditTabs.CreateFromProcess(pid)
				, textEditor: (fileName, displayName, bytes, encoding, modified, forceCreate) => TextEditTabs.Create(fileName, displayName, bytes, encoding, modified, forceCreate: forceCreate)
				, textViewer: (fileName, forceCreate) => TextViewTabs.Create(fileName, forceCreate)
			);

			DispatcherUnhandledException += App_DispatcherUnhandledException;
		}

		void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			ShowExceptionMessage(e.Exception);
			e.Handled = true;
		}
	}
}
