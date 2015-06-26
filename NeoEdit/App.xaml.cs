using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using NeoEdit.Console;
using NeoEdit.DBViewer;
using NeoEdit.Disk;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;
using NeoEdit.Handles;
using NeoEdit.HexEdit;
using NeoEdit.Processes;
using NeoEdit.Registry;
using NeoEdit.SystemInfo;
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
			CreateWindowsFromArgs(String.Join(" ", e.Args.Select(str => "\"" + str + "\"")));
			ShutdownMode = ShutdownMode.OnLastWindowClose;
			if (Application.Current.Windows.Count == 0)
				Application.Current.Shutdown();
		}

		void ShowExceptionMessage(Exception ex)
		{
			var message = "";
			for (var ex2 = ex; ex2 != null; ex2 = ex2.InnerException)
				message += ex2.Message + "\n";
			Message.Show(message, "Error");
#if DEBUG
			if ((Debugger.IsAttached) && ((Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None))
			{
				var inner = ex;
				while (inner.InnerException != null)
					inner = inner.InnerException;
				var er = inner.StackTrace.Split('\r', '\n').FirstOrDefault(a => a.Contains(":line"));
				if (er != null)
				{
					var idx = er.LastIndexOf(" in ");
					if (idx != -1)
						er = er.Substring(idx + 4);
					idx = er.IndexOf(":line ");
					er = er.Substring(0, idx) + " " + er.Substring(idx + 6);
					Clipboard.SetText(er, TextDataFormat.Text);
				}
				System.Diagnostics.Debugger.Break();
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
				getMinimizeToTray: () => NeoEdit.Properties.Settings.Default.MinimizeToTray,
				setMinimizeToTray: value => { NeoEdit.Properties.Settings.Default.MinimizeToTray = value; NeoEdit.Properties.Settings.Default.Save(); },

				systemInfo: () => new SystemInfoWindow(),
				textEditor: (filename, bytes, encoding, modified, createNew) => TextEditTabs.Create(filename, bytes, encoding, modified, createNew: createNew),
				textViewer: (filename, createNew) => TextViewerTabs.Create(filename, createNew),
				fileHexEditor: (filename, binarydata, encoder, modified, createNew) => HexEditTabs.CreateFromFile(filename, binarydata, encoder, modified, createNew),
				processHexEditor: (pid) => HexEditTabs.CreateFromProcess(pid),
				disk: () => new DiskTabs(),
				console: () => new ConsoleTabs(),
				processes: (pid) => new ProcessesWindow(pid),
				handles: (pid) => new HandlesWindow(pid),
				registry: (key) => new RegistryWindow(key),
				dbViewer: () => new DBViewerWindow()
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
