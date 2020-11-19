using System.Windows;

namespace NeoEdit.UI
{
	partial class NEMenu
	{
		public NEMenu() => InitializeComponent();

		void OnStopTasks(object sender, RoutedEventArgs e) => NEGlobalUI.StopTasks();
		void OnKillTasks(object sender, RoutedEventArgs e) => NEGlobalUI.KillTasks();
	}
}
