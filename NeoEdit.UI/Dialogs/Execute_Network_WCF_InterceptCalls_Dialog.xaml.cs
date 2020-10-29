using System.Windows;

namespace NeoEdit.UI.Dialogs
{
	partial class Execute_Network_WCF_InterceptCalls_Dialog
	{
		public Execute_Network_WCF_InterceptCalls_Dialog() => InitializeComponent();

		public static void Run(Window window) => new Execute_Network_WCF_InterceptCalls_Dialog { Owner = window }.ShowDialog();
	}
}
