using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using NeoEdit.Common;
using NeoEdit.GUI.Misc;

namespace NeoEdit.GUI.Controls
{
	public class NEWindow : Window
	{
		bool shiftDown => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

		static readonly string settingsFile = Path.Combine(Helpers.NeoEditAppData, "Settings.xml");

		static NEWindow()
		{
			if (File.Exists(settingsFile))
			{
				try
				{
					var xml = XElement.Load(settingsFile);
					try { minimizeToTray = bool.Parse(xml.Element(nameof(MinimizeToTray)).Value); } catch { }
					try { escapeClearsSelections = bool.Parse(xml.Element(nameof(EscapeClearsSelections)).Value); } catch { }
					try { ripDirectory = xml.Element(nameof(RipDirectory)).Value; } catch { }
					try { youTubeDLPath = xml.Element(nameof(YouTubeDLPath)).Value; } catch { }
					try { streamSaveDirectory = xml.Element(nameof(StreamSaveDirectory)).Value; } catch { }
					try { Font.FontSize = int.Parse(xml.Element(nameof(Font.FontSize)).Value); } catch { }
				}
				catch { }
			}

			Font.FontSizeChanged += newSize => SaveSettings();
		}

		static void SaveSettings()
		{
			try
			{
				var xml = new XElement("Settings");
				xml.Add(new XElement(nameof(MinimizeToTray), minimizeToTray));
				xml.Add(new XElement(nameof(EscapeClearsSelections), escapeClearsSelections));
				xml.Add(new XElement(nameof(RipDirectory), ripDirectory));
				xml.Add(new XElement(nameof(YouTubeDLPath), youTubeDLPath));
				xml.Add(new XElement(nameof(StreamSaveDirectory), streamSaveDirectory));
				xml.Add(new XElement(nameof(Font.FontSize), Font.FontSize));
				xml.Save(settingsFile);
			}
			catch { }
		}

		public NEWindow()
		{
			HelpMenuItem.RegisterCommands(this, (command, multiStatus) => RunCommand(command));
			WindowMenuItem.RegisterCommands(this, (command, multiStatus) => RunCommand(command));
		}

		void RunCommand(HelpCommand command)
		{
			switch (command)
			{
				case HelpCommand.Help_About: Launcher.Static.LaunchAbout(); break;
				case HelpCommand.Help_License: Launcher.Static.LaunchLicense(); break;
				case HelpCommand.Help_Update: Launcher.Static.LaunchUpdate(); break;
				case HelpCommand.Help_RunGC: GC.Collect(); break;
			}
		}

		void RunCommand(WindowCommand command)
		{
			switch (command)
			{
				case WindowCommand.Window_Diff: Launcher.Static.LaunchTextEditorDiff(); break;
				case WindowCommand.Window_Disk: Launcher.Static.LaunchDisk(forceCreate: true); break;
				case WindowCommand.Window_Handles: Launcher.Static.LaunchHandles(); break;
				case WindowCommand.Window_HexEditor: Launcher.Static.LaunchHexEditorFile(forceCreate: true); break;
				case WindowCommand.Window_ImageEditor: Launcher.Static.LaunchImageEditor(); break;
				case WindowCommand.Window_Network: Launcher.Static.LaunchNetwork(); break;
				case WindowCommand.Window_Processes: Launcher.Static.LaunchProcesses(); break;
				case WindowCommand.Window_Ripper: Launcher.Static.LaunchRipper(); break;
				case WindowCommand.Window_StreamSaver: Launcher.Static.LaunchStreamSaver(); break;
				case WindowCommand.Window_TextEditor: Launcher.Static.LaunchTextEditorFile(forceCreate: true); break;
				case WindowCommand.Window_TextViewer: Launcher.Static.LaunchTextViewer(forceCreate: true); break;
			}

			if (shiftDown)
				Close();
		}

		static bool escapeClearsSelections = true;
		public static bool EscapeClearsSelections
		{
			get { return escapeClearsSelections; }
			set
			{
				escapeClearsSelections = value;
				SaveSettings();
				escapeClearsSelectionsChanged?.Invoke(null, new EventArgs());
			}
		}

		static EventHandler escapeClearsSelectionsChanged;
		public static event EventHandler EscapeClearsSelectionsChanged { add { escapeClearsSelectionsChanged += value; } remove { escapeClearsSelectionsChanged -= value; } }

		static bool minimizeToTray = false;
		public static bool MinimizeToTray
		{
			get { return minimizeToTray; }
			set
			{
				minimizeToTray = value;
				SaveSettings();
				minimizeToTrayChanged?.Invoke(null, new EventArgs());
			}
		}

		static EventHandler minimizeToTrayChanged;
		public static event EventHandler MinimizeToTrayChanged { add { minimizeToTrayChanged += value; } remove { minimizeToTrayChanged -= value; } }

		static string ripDirectory = Directory.GetCurrentDirectory();
		public static string RipDirectory
		{
			get { return ripDirectory; }
			set
			{
				ripDirectory = value;
				SaveSettings();
			}
		}

		static EventHandler ripDirectoryChanged;
		public static event EventHandler RipDirectoryChanged { add { ripDirectoryChanged += value; } remove { ripDirectoryChanged -= value; } }

		static string youTubeDLPath = "";
		public static string YouTubeDLPath
		{
			get { return youTubeDLPath; }
			set
			{
				youTubeDLPath = value;
				SaveSettings();
			}
		}

		static EventHandler youTubeDLPathChanged;
		public static event EventHandler YouTubeDLPathChanged { add { youTubeDLPathChanged += value; } remove { youTubeDLPathChanged -= value; } }

		static string streamSaveDirectory = Directory.GetCurrentDirectory();
		public static string StreamSaveDirectory
		{
			get { return streamSaveDirectory; }
			set
			{
				streamSaveDirectory = value;
				SaveSettings();
			}
		}

		static EventHandler streamSaveDirectoryChanged;
		public static event EventHandler StreamSaveDirectoryChanged { add { streamSaveDirectoryChanged += value; } remove { streamSaveDirectoryChanged -= value; } }

		System.Windows.Forms.NotifyIcon ni;
		protected override void OnStateChanged(EventArgs e)
		{
			base.OnStateChanged(e);
			if (WindowState == WindowState.Minimized)
			{
				if (MinimizeToTray)
				{
					ni = new System.Windows.Forms.NotifyIcon
					{
						Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetEntryAssembly().Location),
						Visible = true,
					};
					ni.Click += (s, e2) => Restore();
					Hide();
				}
			}
		}

		public bool Restore()
		{
			if (ni == null)
				return false;

			base.Show();
			WindowState = WindowState.Normal;
			ni.Dispose();
			ni = null;
			return true;
		}
	}
}
