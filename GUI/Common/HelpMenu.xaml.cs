using System.Windows;
using System.Windows.Controls;

namespace NeoEdit.GUI.Common
{
	partial class HelpMenu : MenuItem
	{
		public HelpMenu()
		{
			InitializeComponent();
		}

		void HelpAbout(object sender, RoutedEventArgs e)
		{
			new AboutUI.About().ShowDialog();
		}
	}
}
