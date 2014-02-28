using System.IO;
using System.Windows;
using System.Windows.Threading;
using NeoEdit.GUI.Data;

namespace NeoEdit.GUI
{
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			if (e.Args.Length > 0)
				switch (e.Args[0])
				{
					case "text": new TextEditorUI.TextEditor(); return;
					case "binary": new BinaryEditorUI.BinaryEditor(); return;
					case "gzip":
						{
							var data = File.ReadAllBytes(e.Args[1]);
							data = Compression.Compress(Compression.Type.GZip, data);
							File.WriteAllBytes(e.Args[2], data);
							Application.Current.Shutdown();
						}
						return;
					case "gunzip":
						{
							var data = File.ReadAllBytes(e.Args[1]);
							data = Compression.Decompress(Compression.Type.GZip, data);
							File.WriteAllBytes(e.Args[2], data);
							Application.Current.Shutdown();
						}
						return;
				}

			new BrowserUI.Browser();
		}

		App()
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
