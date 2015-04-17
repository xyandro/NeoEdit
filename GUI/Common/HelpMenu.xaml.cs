using System;
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
			About.AboutWindow.Run();
		}

		void RunGC(object sender, RoutedEventArgs e)
		{
			GC.Collect();
		}
	}
}
