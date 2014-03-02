using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using NeoEdit.GUI.Data;
using NeoEdit.GUI.Records;

namespace NeoEdit.GUI
{
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			try
			{
				var run = true;
				if (e.Args.Length > 0)
				{
					run = false;
					switch (e.Args[0])
					{
						case "text":
							if (e.Args.Length == 1)
								new TextEditorUI.TextEditor();
							else
							{
								var record = new Root().GetRecord(e.Args[1]);
								if (record != null)
									new TextEditorUI.TextEditor(record);
							}
							break;
						case "binary":
							if (e.Args.Length == 1)
								new BinaryEditorUI.BinaryEditor();
							else
							{
								var record = new Root().GetRecord(e.Args[1]);
								if (record != null)
									new BinaryEditorUI.BinaryEditor(record);
							}
							break;
						case "gzip":
							{
								var data = File.ReadAllBytes(e.Args[1]);
								data = Compression.Compress(Compression.Type.GZip, data);
								File.WriteAllBytes(e.Args[2], data);
							}
							break;
						case "gunzip":
							{
								var data = File.ReadAllBytes(e.Args[1]);
								data = Compression.Decompress(Compression.Type.GZip, data);
								File.WriteAllBytes(e.Args[2], data);
							}
							break;
						default: run = true; break;
					}
				}

				if (run)
					new BrowserUI.Browser();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error");
			}

			if (Application.Current.Windows.Count == 0)
				Application.Current.Shutdown();
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
