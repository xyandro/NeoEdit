using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using NeoEdit.GUI.Data;
using NeoEdit.GUI.Records;

namespace NeoEdit.GUI
{
	class InstanceManager : Microsoft.VisualBasic.ApplicationServices.WindowsFormsApplicationBase
	{
		[STAThread]
		static void Main(string[] args) { new InstanceManager().Run(args); }

		App app;

		public InstanceManager() { IsSingleInstance = true; }
		protected override bool OnStartup(Microsoft.VisualBasic.ApplicationServices.StartupEventArgs eventArgs)
		{
			app = new App();
			app.Run();
			return false;
		}

		protected override void OnStartupNextInstance(Microsoft.VisualBasic.ApplicationServices.StartupNextInstanceEventArgs eventArgs)
		{
			base.OnStartupNextInstance(eventArgs);
			app.HandleParams(eventArgs.CommandLine.ToArray());
		}
	}

	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			HandleParams(e.Args);

			if (Application.Current.Windows.Count == 0)
				Application.Current.Shutdown();
		}

		public void HandleParams(string[] args)
		{
			try
			{
				if (args.Length == 0)
				{
					new BrowserUI.Browser();
					return;
				}

				switch (args[0])
				{
					case "text":
						if (args.Length == 1)
							new TextEditorUI.TextEditor();
						else
						{
							var record = new Root().GetRecord(args[1]);
							if (record != null)
								new TextEditorUI.TextEditor(record);
						}
						break;
					case "binary":
						if (args.Length == 1)
							new BinaryEditorUI.BinaryEditor();
						else
						{
							var record = new Root().GetRecord(args[1]);
							if (record != null)
								new BinaryEditorUI.BinaryEditor(record);
						}
						break;
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
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error");
			}
		}

		public App()
		{
			DispatcherUnhandledException += App_DispatcherUnhandledException;
			System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(NeoEdit.GUI.Records.Processes.ProcessRecord).TypeHandle);
		}

		void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			MessageBox.Show(e.Exception.Message, "Error");
			e.Handled = true;
		}
	}
}
