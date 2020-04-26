﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using NeoEdit.Common;
using NeoEdit.UI.Dialogs;

namespace NeoEdit.UI
{
	partial class App
	{
		static App app;

		Action startAction;
		App(Action action)
		{
			ITabsWindowStatic.RunCryptorKeyDialog = (type, encrypt) => CryptorKeyDialog.Run(null, type, encrypt)?.Key;
			ITabsWindowStatic.CreateITabsWindow = tabs => Dispatcher.Invoke(() => new TabsWindow(tabs));
			ITabsWindowStatic.ShowExceptionMessage = ex => TabsWindow.ShowExceptionMessage(ex);

			startAction = action;
			InitializeComponent();
			DispatcherUnhandledException += App_DispatcherUnhandledException;
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			EventManager.RegisterClassHandler(typeof(TextBox), TextBox.GotFocusEvent, new RoutedEventHandler((s, e2) => (s as TextBox).SelectAll()));
			base.OnStartup(e);
			// Without the ShutdownMode lines, the program will close if a dialog is displayed and closed before any windows
			ShutdownMode = ShutdownMode.OnExplicitShutdown;
			startAction();
		}

		void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			TabsWindow.ShowExceptionMessage(e.Exception);
			e.Handled = true;
		}

		public static void Run(Action action)
		{
			if (app != null)
			{
				app.Dispatcher.Invoke(action);
				return;
			}

			app = new App(action);
			app.Run();
		}
	}
}
