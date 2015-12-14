using System.Windows;

namespace NeoEdit.Tools.Dialogs.NSRLTool
{
	partial class NSRLToolDialog
	{
		NSRLToolDialog()
		{
			InitializeComponent();
		}

		void OkClick(object sender, RoutedEventArgs e) => DialogResult = true;

		private void CreateIndex(object sender, RoutedEventArgs e) => CreateIndexDialog.Run();

		private void LookupValues(object sender, RoutedEventArgs e) => LookupValuesDialog.Run();

		private void LookupFiles(object sender, RoutedEventArgs e) => LookupFilesDialog.Run();

		public static void Run()
		{
			new NSRLToolDialog().ShowDialog();
		}
	}
}
