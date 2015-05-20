using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace NeoEdit.GUI.Common
{
	public class NEWindow : Window
	{
		public NEWindow()
		{
			HelpMenuItem.RegisterCommands(this, (s, e, command) => RunCommand(command));
			WindowMenuItem.RegisterCommands(this, (s, e, command) => RunCommand(command));
		}

		void RunCommand(HelpCommand command)
		{
			switch (command)
			{
				case HelpCommand.Help_About: About.AboutWindow.Run(); break;
				case HelpCommand.Help_RunGC: GC.Collect(); break;
			}
		}

		void RunCommand(WindowCommand command)
		{
			switch (command)
			{
				case WindowCommand.Window_Clipboard: ClipboardWindow.Show(); break;
				case WindowCommand.Window_Console: Launcher.Static.LaunchConsole(); break;
				case WindowCommand.Window_DBViewer: Launcher.Static.LaunchDBViewer(); break;
				case WindowCommand.Window_Disk: Launcher.Static.LaunchDisk(); break;
				case WindowCommand.Window_Handles: Launcher.Static.LaunchHandles(); break;
				case WindowCommand.Window_HexEditor: Launcher.Static.LaunchHexEditor(createNew: true); break;
				case WindowCommand.Window_Processes: Launcher.Static.LaunchProcesses(); break;
				case WindowCommand.Window_Registry: Launcher.Static.LaunchRegistry(); break;
				case WindowCommand.Window_SystemInfo: Launcher.Static.LaunchSystemInfo(); break;
				case WindowCommand.Window_TextEditor: Launcher.Static.LaunchTextEditor(createNew: true); break;
				case WindowCommand.Window_TextViewer: Launcher.Static.LaunchTextViewer(); break;
			}
		}

		public static bool MinimizeToTray
		{
			get
			{
				return Launcher.Static.MinimizeToTray;
			}
			set
			{
				Launcher.Static.MinimizeToTray = value;
				if (minimizeToTrayChanged != null)
					minimizeToTrayChanged(null, new EventArgs());
			}
		}

		static EventHandler minimizeToTrayChanged;
		public static event EventHandler MinimizeToTrayChanged { add { minimizeToTrayChanged += value; } remove { minimizeToTrayChanged -= value; } }

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
