using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using NeoEdit.Common;
using NeoEdit.UI.Dialogs;

namespace NeoEdit.UI
{
	partial class App
	{
		static App app;

		static App()
		{
			NoWindowDialogs.RunCryptorKeyDialog = (type, encrypt) => CryptorKeyDialog.Run(null, type, encrypt);
		}

		Action startAction;
		App(Action action)
		{
			ITabsWindowStatic.CreateITabsWindow = tabs => Dispatcher.Invoke(() => new TabsWindow(tabs));
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

		public static void ShowExceptionMessage(Exception ex)
		{
			var message = "";
			for (var ex2 = ex; ex2 != null; ex2 = ex2.InnerException)
				message += $"{ex2.Message}\n";

			var window = Current.Windows.OfType<Window>().SingleOrDefault(w => w.IsActive);
			Message.Run(window, "Error", message);

			if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
			{
				var exceptions = new List<Exception> { ex };
				while (exceptions[0].InnerException != null)
					exceptions.Insert(0, exceptions[0].InnerException);

				if ((Helpers.IsDebugBuild) && (Debugger.IsAttached))
				{
					var inner = exceptions.First();
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
				else
				{
					var sb = new StringBuilder();
					var first = true;
					foreach (var exception in exceptions)
					{
						if (first)
							first = false;
						else
							sb.AppendLine();
						sb.AppendLine($"Message: {exception?.Message ?? ""}");
						sb.AppendLine($"StackTrace:\r\n{exception?.StackTrace ?? ""}");
					}
					Clipboard.SetText(sb.ToString());
				}
			}
		}

		void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			ShowExceptionMessage(e.Exception);
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
