using System;
using System.Windows;

namespace NeoEdit.UI
{
	partial class NEMenu
	{
		public event Action StopTasks;
		public event Action KillTasks;

		public NEMenu() => InitializeComponent();

		void OnStopTasks(object sender, RoutedEventArgs e) => StopTasks?.Invoke();
		void OnKillTasks(object sender, RoutedEventArgs e) => KillTasks?.Invoke();
	}
}
