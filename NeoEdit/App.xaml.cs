using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using NeoEdit.Program.CommandLine;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Dialogs;

namespace NeoEdit.Program
{
	partial class App
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			EventManager.RegisterClassHandler(typeof(TextBox), TextBox.GotFocusEvent, new RoutedEventHandler((s, e2) => (s as TextBox).SelectAll()));
			base.OnStartup(e);
			// Without the ShutdownMode lines, the program will close if a dialog is displayed and closed before any windows
			ShutdownMode = ShutdownMode.OnExplicitShutdown;
			CreateWindowsFromArgs(string.Join(" ", e.Args.Select(str => $"\"{str}\"")), true);
		}

		void ShowExceptionMessage(Exception ex)
		{
			var message = "";
			for (var ex2 = ex; ex2 != null; ex2 = ex2.InnerException)
				message += $"{ex2.Message}\n";

			var window = Current.Windows.OfType<Window>().SingleOrDefault(w => w.IsActive);
			Message.Show(message, "Error", window);

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

		public TabsWindow CreateWindowsFromArgs(string commandLine, bool shutdownIfNoWindow)
		{
			try
			{
				var clParams = CommandLineVisitor.GetCommandLineParams(commandLine);
				if (clParams.Background)
					return null;

				if (!clParams.Files.Any())
					return new TabsWindow(true);

				var shutdownData = string.IsNullOrWhiteSpace(clParams.Wait) ? null : new ShutdownData(clParams.Wait, clParams.Files.Count);
				var tabsWindow = default(TabsWindow);
				if (!clParams.Diff)
					tabsWindow = UIHelper<TabsWindow>.GetAllWindows().OrderByDescending(x => x.LastActivated).FirstOrDefault();
				if (tabsWindow == null)
					tabsWindow = new TabsWindow();
				foreach (var file in clParams.Files)
					tabsWindow.AddTextEditor(new TextEditor(file.FileName, file.DisplayName, line: file.Line, column: file.Column, index: file.Index, shutdownData: shutdownData));
				if (tabsWindow.Tabs.Any())
					tabsWindow.SetFocused(tabsWindow.Tabs[tabsWindow.Tabs.Count - 1], true);

				if (clParams.Diff)
				{
					for (var ctr = 0; ctr + 1 < tabsWindow.Tabs.Count; ctr += 2)
					{
						tabsWindow.Tabs[ctr].DiffTarget = tabsWindow.Tabs[ctr + 1];
						if (tabsWindow.Tabs[ctr].ContentType == ParserType.None)
							tabsWindow.Tabs[ctr].ContentType = tabsWindow.Tabs[ctr + 1].ContentType;
						if (tabsWindow.Tabs[ctr + 1].ContentType == ParserType.None)
							tabsWindow.Tabs[ctr + 1].ContentType = tabsWindow.Tabs[ctr].ContentType;
					}
					tabsWindow.SetLayout(maxColumns: 2);
				}

				return tabsWindow;
			}
			catch (Exception ex) { ShowExceptionMessage(ex); }
			if ((shutdownIfNoWindow) && (Current.Windows.Count == 0))
				Current.Shutdown();
			return null;
		}

		public App()
		{
			InitializeComponent();
			DispatcherUnhandledException += App_DispatcherUnhandledException;
		}

		void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			ShowExceptionMessage(e.Exception);
			e.Handled = true;
		}
	}
}
