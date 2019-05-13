﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
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
				var paramList = CommandLineParams.CommandLineVisitor.GetCommandLineParams(commandLine);
				var shutdownEvent = paramList.OfType<WaitParam>().FirstOrDefault()?.ShutdownEvent;
				paramList.RemoveAll(param => param is WaitParam);

				var windows = UIHelper<NEWindow>.GetAllWindows();
				var restored = windows.Count(window => window.Restore());
				if ((restored > 0) && (!paramList.Any()))
					return null;

				if (!paramList.Any())
					return TextEditTabs.Create(shutdownEvent: shutdownEvent);
				Window created = null;
				foreach (var param in paramList)
					created = param.Execute(shutdownEvent);
				return created;
			}
			catch (Exception ex) { ShowExceptionMessage(ex); }
			return null;
		}

		public App()
		{
			NeoEdit.Launcher.Initialize(
				about: () => AboutDialog.Run()
				, textEditorDiff: (fileName1, displayName1, bytes1, codePage1, modified1, line1, column1, fileName2, displayName2, bytes2, codePage2, modified2, line2, column2, shutdownEvent) => new TextEditTabs().AddDiff(fileName1, displayName1, bytes1, codePage1, Content.Parser.ParserType.None, modified1, line1, column1, fileName2, displayName2, bytes2, codePage2, Content.Parser.ParserType.None, modified2, line2, column2, shutdownEvent)
				, textEditorFile: (fileName, displayName, bytes, encoding, modified, line, column, forceCreate, shutdownEvent) => TextEditTabs.Create(fileName, displayName, bytes, encoding, Content.Parser.ParserType.None, modified, line ?? 1, column ?? 1, null, forceCreate, shutdownEvent)
				, update: () => Update()
			);

			DispatcherUnhandledException += App_DispatcherUnhandledException;
		}

		void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			ShowExceptionMessage(e.Exception);
			e.Handled = true;
		}

		static void Update()
		{
			const string url = "https://github.com/xyandro/NeoEdit/releases/latest";
			const string check = "https://github.com/xyandro/NeoEdit/releases/tag/";
			const string exe = "https://github.com/xyandro/NeoEdit/releases/download/{0}/NeoEdit.exe";

			var oldVersion = ((AssemblyFileVersionAttribute)typeof(App).Assembly.GetCustomAttribute(typeof(AssemblyFileVersionAttribute))).Version;
			string newVersion;

			var request = WebRequest.Create(url) as HttpWebRequest;
			request.AllowAutoRedirect = false;
			using (var response = request.GetResponse() as HttpWebResponse)
			{
				var redirUrl = response.Headers["Location"];
				if (!redirUrl.StartsWith(check))
					throw new Exception("Version check failed to find latest version");

				newVersion = redirUrl.Substring(check.Length);
			}

			var oldNums = oldVersion.Split('.').Select(str => int.Parse(str)).ToList();
			var newNums = newVersion.Split('.').Select(str => int.Parse(str)).ToList();
			if (oldNums.Count != newNums.Count)
				throw new Exception("Version length mismatch");

			var newer = oldNums.Zip(newNums, (oldNum, newNum) => newNum.IsGreater(oldNum)).NonNull().FirstOrDefault();
			if (new Message
			{
				Title = "Download new version?",
				Text = newer ? $"A newer version ({newVersion}) is available.  Download it?" : $"Already up to date ({newVersion}).  Update anyway?",
				Options = Message.OptionsEnum.YesNo,
				DefaultAccept = newer ? Message.OptionsEnum.Yes : Message.OptionsEnum.No,
				DefaultCancel = Message.OptionsEnum.No,
			}.Show() != Message.OptionsEnum.Yes)
				return;

			var oldLocation = Assembly.GetEntryAssembly().Location;
			var newLocation = Path.Combine(Path.GetDirectoryName(oldLocation), $"{Path.GetFileNameWithoutExtension(oldLocation)}-Update{Path.GetExtension(oldLocation)}");

			byte[] result = null;
			ProgressDialog.Run(null, "Downloading new version...", (cancelled, progress) =>
			{
				var finished = new ManualResetEvent(false);
				using (var client = new WebClient())
				{
					client.DownloadProgressChanged += (s, e) => progress(e.ProgressPercentage);
					client.DownloadDataCompleted += (s, e) =>
					{
						if (!e.Cancelled)
							result = e.Result;
						finished.Set();
					};
					client.DownloadDataAsync(new Uri(string.Format(exe, newVersion)));
					while (!finished.WaitOne(500))
						if (cancelled())
							client.CancelAsync();
				}
			});

			if (result == null)
				return;

			File.WriteAllBytes(newLocation, result);

			Message.Show("The program will be updated after exiting.");
			Process.Start(newLocation, $@"-update ""{oldLocation}"" {Process.GetCurrentProcess().Id}");
		}
	}
}
