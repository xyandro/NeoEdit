﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using NeoEdit.Content;
using NeoEdit.Controls;
using NeoEdit.Dialogs;

namespace NeoEdit
{
	partial class App
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			EventManager.RegisterClassHandler(typeof(TextBox), TextBox.GotFocusEvent, new RoutedEventHandler((s, e2) => (s as TextBox).SelectAll()));
			base.OnStartup(e);
			// Without the ShutdownMode lines, the program will close if a dialog is displayed and closed before any windows
			ShutdownMode = ShutdownMode.OnExplicitShutdown;
			CreateWindowsFromArgs(string.Join(" ", e.Args.Select(str => $"\"{str}\"")));
			ShutdownMode = ShutdownMode.OnLastWindowClose;
			if (Current.Windows.Count == 0)
				Current.Shutdown();
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

		public Window CreateWindowsFromArgs(string commandLine)
		{
			try
			{
				var clParams = CommandLine.CommandLineVisitor.GetCommandLineParams(commandLine);

				var windows = UIHelper<NEWindow>.GetAllWindows();
				var restored = windows.Count(window => window.Restore());

				if (!clParams.Files.Any())
				{
					if (restored > 0)
						return null;
					return Tabs.Create(shutdownEvent: clParams.Wait);
				}

				var tabs = new Tabs();
				foreach (var file in clParams.Files)
					tabs.AddTab(new TextEditor(file.FileName, file.DisplayName, line: file.Line, column: file.Column, shutdownData: new ShutdownData(clParams.Wait, 1)));

				if (clParams.Diff)
				{
					for (var ctr = 0; ctr + 1 < tabs.Items.Count; ctr += 2)
					{
						tabs.Items[ctr].DiffTarget = tabs.Items[ctr + 1];
						if (tabs.Items[ctr].ContentType == Parser.ParserType.None)
							tabs.Items[ctr].ContentType = tabs.Items[ctr + 1].ContentType;
						if (tabs.Items[ctr + 1].ContentType == Parser.ParserType.None)
							tabs.Items[ctr + 1].ContentType = tabs.Items[ctr].ContentType;
					}
					tabs.SetLayout(TabsLayout.Custom, 2);
				}

				return tabs;
			}
			catch (Exception ex) { ShowExceptionMessage(ex); }
			return null;
		}

		public App()
		{
			DispatcherUnhandledException += App_DispatcherUnhandledException;
		}

		void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			ShowExceptionMessage(e.Exception);
			e.Handled = true;
		}
	}
}
