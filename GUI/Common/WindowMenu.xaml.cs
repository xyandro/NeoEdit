using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NeoEdit.GUI.Common
{
	partial class WindowMenu : MenuItem
	{
		public static RoutedCommand Command_Window_Browser = new RoutedCommand();
		public static RoutedCommand Command_Window_Processes = new RoutedCommand();
		public static RoutedCommand Command_Window_Handles = new RoutedCommand();
		public static RoutedCommand Command_Window_BinaryEditor = new RoutedCommand();
		public static RoutedCommand Command_Window_TextEditor = new RoutedCommand();
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
			if (e.Command == Command_Window_Browser)
				Launcher.Static.LaunchBrowser();
			else if (e.Command == Command_Window_Processes)
				Launcher.Static.LaunchProcesses();
			else if (e.Command == Command_Window_Handles)
				Launcher.Static.LaunchHandles();
			else if (e.Command == Command_Window_BinaryEditor)
				Launcher.Static.LaunchBinaryEditor();
			else if (e.Command == Command_Window_TextEditor)
				Launcher.Static.LaunchTextEditor();
			else if (e.Command == Command_Window_SystemInfo)
				Launcher.Static.LaunchSystemInfo();
		}
	}
}
