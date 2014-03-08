using System.Windows;
using System.Windows.Controls;

namespace NeoEdit.GUI.Common
{
	partial class WindowMenu : MenuItem
	{
		public WindowMenu()
		{
			InitializeComponent();
		}

		void WindowBrowser(object sender, RoutedEventArgs e)
		{
			new Browser.BrowserWindow();
		}

		void WindowBinaryEditor(object sender, RoutedEventArgs e)
		{
			Launcher.Static.LaunchBinaryEditor();
		}

		void WindowTextEditor(object sender, RoutedEventArgs e)
		{
			Launcher.Static.LaunchTextEditor();
		}
	}
}
