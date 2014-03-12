using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using NeoEdit.BinaryEditor;
using NeoEdit.Browser;
using NeoEdit.Common.Transform;
using NeoEdit.Handles;
using NeoEdit.Processes;
using NeoEdit.TextEditor;

namespace NeoEdit
{
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
							string filename = null;
							if (args.Length > 1)
							{
								filename = args[1];
								if (!File.Exists(filename))
									throw new ArgumentException("Invalid file.");
							}

							int? line = null;
							if (args.Length > 2)
								line = Convert.ToInt32(args[2]);

							int? column = null;
							if (args.Length > 3)
								column = Convert.ToInt32(args[3]);

							return new TextEditorWindow(filename, line: line, column: column);
						}
					case "binary":
						{
							string filename = null;
							if (args.Length > 1)
							{
								filename = args[1];
								if (!File.Exists(filename))
									throw new ArgumentException("Invalid file.");
							}

							return BinaryEditorWindow.CreateFromFile(filename);
						}
					case "binarypid":
						{
							if (args.Length < 2)
								throw new ArgumentException("Not enough parameters.");

							return BinaryEditorWindow.CreateFromProcess(Convert.ToInt32(args[1]));
						}
					case "binarydump":
						{
							if (args.Length < 2)
								throw new ArgumentException("Not enough parameters.");

							return BinaryEditorWindow.CreateFromDump(args[1]);
						}
					case "process":
					case "processes":
						{
							int? pid = null;
							if (args.Length > 1)
								pid = Convert.ToInt32(args[1]);
							return new ProcessesWindow(pid);
						}
					case "handle":
					case "handles":
						{
							int? pid = null;
							if (args.Length > 1)
								pid = Convert.ToInt32(args[1]);
							return new HandlesWindow(pid);
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
			InitializeComponent();

			NeoEdit.GUI.Launcher.Initialize(
				textEditor: (filename, bytes, encoding) => new TextEditorWindow(filename, bytes, encoding),
				fileBinaryEditor: (filename, binarydata) => BinaryEditorWindow.CreateFromFile(filename, binarydata),
				processBinaryEditor: (pid) => BinaryEditorWindow.CreateFromProcess(pid),
				browser: () => new BrowserWindow(),
				processes: (pid) => new ProcessesWindow(pid),
				handles: (pid) => new HandlesWindow(pid)
			);

			DispatcherUnhandledException += App_DispatcherUnhandledException;
			System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(NeoEdit.Processes.ProcessItem).TypeHandle);
		}

		void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			ShowExceptionMessage(e.Exception);
			e.Handled = true;
		}
	}
}
