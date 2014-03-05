using System.Windows;
using System.Windows.Controls;

namespace NeoEdit.GUI.Common
{
	public partial class WindowMenu : MenuItem
	{
		public WindowMenu()
		{
			InitializeComponent();
		}

		void WindowBrowser(object sender, RoutedEventArgs e)
		{
			new BrowserUI.Browser();
		}

		void WindowBinaryEditor(object sender, RoutedEventArgs e)
		{
			new BinaryEditorUI.BinaryEditor();
		}

		void WindowTextEditor(object sender, RoutedEventArgs e)
		{
			new TextEditorUI.TextEditor();
		}
	}
}
