using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NeoEdit.GUI.Common
{
	partial class WindowMenu : MenuItem
	{
		public static RoutedCommand Command_Window_Disk = new RoutedCommand();
		public static RoutedCommand Command_Window_Console = new RoutedCommand();
		public static RoutedCommand Command_Window_Processes = new RoutedCommand();
		public static RoutedCommand Command_Window_Handles = new RoutedCommand();
		public static RoutedCommand Command_Window_Registry = new RoutedCommand();
		public static RoutedCommand Command_Window_DBViewer = new RoutedCommand();
		public static RoutedCommand Command_Window_BinaryEditor = new RoutedCommand();
		public static RoutedCommand Command_Window_TextEditor = new RoutedCommand();
		public static RoutedCommand Command_Window_TextViewer = new RoutedCommand();
		public static RoutedCommand Command_Window_SystemInfo = new RoutedCommand();

		public static DependencyProperty ParentWindowProperty = DependencyProperty.Register("ParentWindow", typeof(Window), typeof(WindowMenu), new PropertyMetadata((d, e) => (d as WindowMenu).OnParentWindowChanged()));

		public Window ParentWindow { get { return (Window)GetValue(ParentWindowProperty); } set { SetValue(ParentWindowProperty, value); } }

		public WindowMenu()
		{
			InitializeComponent();
		}

		void OnParentWindowChanged()
		{
			foreach (CommandBinding commandBinding in CommandBindings)
				ParentWindow.CommandBindings.Add(commandBinding);
			foreach (InputBinding inputBinding in InputBindings)
				ParentWindow.InputBindings.Add(inputBinding);
		}

		void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (e.Command == Command_Window_Disk)
				Launcher.Static.LaunchDisk();
			else if (e.Command == Command_Window_Console)
				Launcher.Static.LaunchConsole();
			else if (e.Command == Command_Window_Processes)
				Launcher.Static.LaunchProcesses();
			else if (e.Command == Command_Window_Handles)
				Launcher.Static.LaunchHandles();
			else if (e.Command == Command_Window_Registry)
				Launcher.Static.LaunchRegistry();
			else if (e.Command == Command_Window_DBViewer)
				Launcher.Static.LaunchDBViewer();
			else if (e.Command == Command_Window_BinaryEditor)
				Launcher.Static.LaunchBinaryEditor(createNew: true);
			else if (e.Command == Command_Window_TextEditor)
				Launcher.Static.LaunchTextEditor(createNew: true);
			else if (e.Command == Command_Window_TextViewer)
				Launcher.Static.LaunchTextViewer();
			else if (e.Command == Command_Window_SystemInfo)
				Launcher.Static.LaunchSystemInfo();
		}
	}
}
