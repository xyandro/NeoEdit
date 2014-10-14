using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using NeoEdit.BinaryEditor;
using NeoEdit.Common.Transform;
using NeoEdit.Console;
using NeoEdit.DBViewer;
using NeoEdit.Disk;
using NeoEdit.GUI.About;
using NeoEdit.GUI.Dialogs;
using NeoEdit.Handles;
using NeoEdit.Processes;
using NeoEdit.Registry;
using NeoEdit.SystemInfo;
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
			for (var ex2 = ex; ex2 != null; ex2 = ex2.InnerException)
				message += ex2.Message + "\n";
			Message.Show(message, "Error");
#if DEBUG
			if ((Debugger.IsAttached) && ((Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None))
			{
				var inner = ex;
				while (inner.InnerException != null)
					inner = inner.InnerException;
				var er = inner.StackTrace.Split('\r', '\n').FirstOrDefault(a => a.Contains(":line"));
				if (er != null)
				{
					var idx = er.LastIndexOf(" in ");
					if (idx != -1)
						er = er.Substring(idx + 4);
					idx = er.IndexOf(":line ");
					er = er.Substring(0, idx) + " " + er.Substring(idx + 6);
					Clipboard.SetText(er, TextDataFormat.Text);
				}
				System.Diagnostics.Debugger.Break();
			}
#endif

		}

		public Window GetWindowFromArgs(string[] args)
		{
			try
			{
				args = args.Where(arg => arg != "multi").ToArray();
				if (args.Length == 0)
					return new DiskWindow();

				switch (args[0])
				{
					case "about":
						return new AboutWindow();
					case "system":
					case "systeminfo":
						return new SystemInfoWindow();
					case "console":
						return new ConsoleTabs();
					case "consolerunner":
						{
							new Console.ConsoleRunner(args.Skip(1).ToArray());
							return null;
						}
					case "disk":
					case "disks":
						{
							string location = null;
							if (args.Length > 1)
								location = args[1];
							return new DiskWindow(location);
						}
					case "text":
					case "textedit":
					case "texteditor":
						{
							string filename = null;
							if (args.Length > 1)
							{
								filename = args[1];
								if (!File.Exists(filename))
									throw new ArgumentException("Invalid file.");
							}

							int line = 1;
							if (args.Length > 2)
								line = Convert.ToInt32(args[2]);

							int column = 1;
							if (args.Length > 3)
								column = Convert.ToInt32(args[3]);

							return new TextEditorTabs(filename, line: line, column: column);
						}
					case "binary":
					case "binaryedit":
					case "binaryeditor":
						{
							string filename = null;
							if (args.Length > 1)
							{
								filename = args[1];
								if (!File.Exists(filename))
									throw new ArgumentException("Invalid file.");
							}

							return BinaryEditorTabs.CreateFromFile(filename);
						}
					case "binarypid":
						{
							if (args.Length < 2)
								throw new ArgumentException("Not enough parameters.");

							return BinaryEditorTabs.CreateFromProcess(Convert.ToInt32(args[1]));
						}
					case "binarydump":
						{
							if (args.Length < 2)
								throw new ArgumentException("Not enough parameters.");

							return BinaryEditorTabs.CreateFromDump(args[1]);
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
					case "registry":
						{
							string key = null;
							if (args.Length > 1)
								key = args[1];
							return new RegistryWindow(key);
						}
					case "db":
					case "dbview":
					case "dbviewer":
						return new DBViewerWindow();
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
					default: throw new Exception("Invalid argument");
				}
			}
			catch (Exception ex) { ShowExceptionMessage(ex); }

			return null;
		}

		public App()
		{
			InitializeComponent();

			NeoEdit.GUI.Launcher.Initialize(
				systemInfo: () => new SystemInfoWindow(),
				textEditor: (filename, bytes, encoding) => new TextEditorTabs(filename, bytes, encoding),
				fileBinaryEditor: (filename, binarydata, encoder) => BinaryEditorTabs.CreateFromFile(filename, binarydata, encoder),
				processBinaryEditor: (pid) => BinaryEditorTabs.CreateFromProcess(pid),
				disk: () => new DiskWindow(),
				console: () => new ConsoleTabs(),
				processes: (pid) => new ProcessesWindow(pid),
				handles: (pid) => new HandlesWindow(pid),
				registry: (key) => new RegistryWindow(key),
				dbViewer: () => new DBViewerWindow()
			);

			DispatcherUnhandledException += App_DispatcherUnhandledException;
		}

		void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			ShowExceptionMessage(e.Exception);
			e.Handled = true;
		}
	}
}
