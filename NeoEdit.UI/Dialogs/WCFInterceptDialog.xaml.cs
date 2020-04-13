using System.Windows;

namespace NeoEdit.UI.Dialogs
{
	partial class WCFInterceptDialog
	{
		public WCFInterceptDialog() => InitializeComponent();

		public static void Run(Window window) => new WCFInterceptDialog { Owner = window }.ShowDialog();
	}
}
