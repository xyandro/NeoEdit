using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using NeoEdit.HexEdit;
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
using NeoEdit.TextEdit;
using NeoEdit.TextView;

namespace NeoEdit
{
	partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			// Without the ShutdownMode lines, the program will close if a dialog is displayed and closed before any windows
			ShutdownMode = ShutdownMode.OnExplicitShutdown;
			CreateWindowFromArgs(e.Args);
			ShutdownMode = ShutdownMode.OnLastWindowClose;
			if (Application.Current.Windows.Count == 0)
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

		public void CreateWindowFromArgs(string[] args)
		{
			try
			{
				args = args.Where(arg => arg != "multi").ToArray();
				if (args.Length == 0)
				{
					new DiskTabs();
					return;
				}

				switch (args[0])
				{
					case "about":
						new AboutWindow();
						return;
					case "system":
					case "systeminfo":
						new SystemInfoWindow();
						return;
					case "console":
						new ConsoleTabs();
						return;
					case "consolerunner":
						new Console.ConsoleRunner(args.Skip(1).ToArray());
						return;
					case "disk":
					case "disks":
						{
							string location = null;
							if (args.Length > 1)
								location = args[1];
							new DiskTabs(location);
							return;
						}
					case "edit":
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

							TextEditTabs.Create(filename, line: line, column: column);
							return;
						}
					case "view":
					case "textview":
					case "textviewer":
						{
							string filename = null;
							if (args.Length > 1)
							{
								filename = args[1];
								if (!File.Exists(filename))
									throw new ArgumentException("Invalid file.");
							}

							TextViewerTabs.Create(filename);
							return;
						}
					case "binary":
					case "binaryedit":
					case "binaryeditor":
					case "hex":
					case "hexedit":
					case "hexeditor":
						{
							string filename = null;
							if (args.Length > 1)
							{
								filename = args[1];
								if (!File.Exists(filename))
									throw new ArgumentException("Invalid file.");
							}

							HexEditTabs.CreateFromFile(filename);
							return;
						}
					case "binarypid":
					case "hexpid":
						{
							if (args.Length < 2)
								throw new ArgumentException("Not enough parameters.");

							HexEditTabs.CreateFromProcess(Convert.ToInt32(args[1]));
							return;
						}
					case "binarydump":
					case "hexdump":
						{
							if (args.Length < 2)
								throw new ArgumentException("Not enough parameters.");

							HexEditTabs.CreateFromDump(args[1]);
							return;
						}
					case "process":
					case "processes":
						{
							int? pid = null;
							if (args.Length > 1)
								pid = Convert.ToInt32(args[1]);
							new ProcessesWindow(pid);
							return;
						}
					case "handle":
					case "handles":
						{
							int? pid = null;
							if (args.Length > 1)
								pid = Convert.ToInt32(args[1]);
							new HandlesWindow(pid);
							return;
						}
					case "registry":
						{
							string key = null;
							if (args.Length > 1)
								key = args[1];
							new RegistryWindow(key);
							return;
						}
					case "db":
					case "dbview":
					case "dbviewer":
						new DBViewerWindow();
						return;
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
		}

		public App()
		{
			InitializeComponent();

			NeoEdit.GUI.Launcher.Initialize(
				systemInfo: () => new SystemInfoWindow(),
				textEditor: (filename, bytes, encoding, createNew) => TextEditTabs.Create(filename, bytes, encoding, createNew: createNew),
				textViewer: (filename, createNew) => TextViewerTabs.Create(filename, createNew),
				fileHexEditor: (filename, binarydata, encoder, createNew) => HexEditTabs.CreateFromFile(filename, binarydata, encoder, createNew),
				processHexEditor: (pid) => HexEditTabs.CreateFromProcess(pid),
				disk: () => new DiskTabs(),
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
