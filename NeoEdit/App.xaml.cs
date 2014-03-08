using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using NeoEdit.BinaryEditor;
using NeoEdit.Browser;
using NeoEdit.Common.Transform;
using NeoEdit.Records;
using NeoEdit.TextEditor;

namespace NeoEdit
{
	class InstanceManager : Microsoft.VisualBasic.ApplicationServices.WindowsFormsApplicationBase
	{
		[STAThread]
		static void Main(string[] args) { new InstanceManager().Run(args); }

		App app;

		InstanceManager() { IsSingleInstance = true; }
		protected override bool OnStartup(Microsoft.VisualBasic.ApplicationServices.StartupEventArgs e)
		{
			app = new App();
			app.Run();
			return false;
		}

		protected override void OnStartupNextInstance(Microsoft.VisualBasic.ApplicationServices.StartupNextInstanceEventArgs e)
		{
			base.OnStartupNextInstance(e);
			var window = app.GetWindowFromArgs(e.CommandLine.ToArray());
			if (window != null)
			{
				window.Activate();
				window.Topmost = true;
				window.Topmost = false;
				window.Focus();
			}
		}
	}

	partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			if (GetWindowFromArgs(e.Args) == null)
				Application.Current.Shutdown();
		}

		void ShowExceptionMessage(Exception ex)
		{
			var message = "";
			for (; ex != null; ex = ex.InnerException)
				message += ex.Message + "\n";
			MessageBox.Show(message, "Error");
		}

		public Window GetWindowFromArgs(string[] args)
		{
			try
			{
				if (args.Length == 0)
					return new BrowserWindow();

				switch (args[0])
				{
					case "text":
						{
							Record record = null;
							if (args.Length > 1)
							{
								record = new Root().GetRecord(args[1]);
								if (record == null)
									throw new ArgumentException("Invalid file.");
							}

							int? line = null;
							if (args.Length > 2)
								line = Convert.ToInt32(args[2]);

							int? column = null;
							if (args.Length > 3)
								column = Convert.ToInt32(args[3]);

							return new TextEditorWindow(record, line: line, column: column);
						}
					case "binary":
						{
							Record record = null;
							if (args.Length > 1)
							{
								record = new Root().GetRecord(args[1]);
								if (record == null)
									throw new ArgumentException("Invalid file.");
							}

							return new BinaryEditorWindow(record);
						}
					case "gzip":
						{
							var data = File.ReadAllBytes(args[1]);
							data = Compression.Compress(Compression.Type.GZip, data);
							File.WriteAllBytes(args[2], data);
						}
						break;
					case "gunzip":
						{
							var data = File.ReadAllBytes(args[1]);
							data = Compression.Decompress(Compression.Type.GZip, data);
							File.WriteAllBytes(args[2], data);
						}
						break;
				}
			}
			catch (Exception ex) { ShowExceptionMessage(ex); }

			return null;
		}

		public App()
		{
			NeoEdit.GUI.Launcher.Initialize(
				_textEditorLauncher: (record, textdata) => new TextEditorWindow(record, textdata),
				_binaryEditorLauncher: (record, binarydata) => new BinaryEditorWindow(record, binarydata),
				_browserLauncher: () => new BrowserWindow()
			);

			DispatcherUnhandledException += App_DispatcherUnhandledException;
			System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(NeoEdit.Records.Processes.ProcessRecord).TypeHandle);
		}

		void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			ShowExceptionMessage(e.Exception);
			e.Handled = true;
		}
	}
}
