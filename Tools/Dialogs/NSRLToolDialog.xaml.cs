using System.Windows;

namespace NeoEdit.Tools.Dialogs
{
	partial class NSRLToolDialog
	{
		NSRLToolDialog()
		{
			InitializeComponent();
		}

		void OkClick(object sender, RoutedEventArgs e) => DialogResult = true;

		private void CreateIndex(object sender, RoutedEventArgs e) => CreateNSRLIndexDialog.Run();

		private void LookupValues(object sender, RoutedEventArgs e) => LookupNSRLValuesDialog.Run();

		public static void Run()
		{
			new NSRLToolDialog().ShowDialog();
		}
	}
}
