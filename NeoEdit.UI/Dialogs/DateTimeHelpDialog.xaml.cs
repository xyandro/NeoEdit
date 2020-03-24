using System.Windows;
using System.Windows.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class DateTimeHelpDialog
	{
		static DateTimeHelpDialog singleton = null;

		DateTimeHelpDialog() { InitializeComponent(); }

		protected override void OnClosed(System.EventArgs e)
		{
			base.OnClosed(e);
			singleton = null;
		}

		void OkClick(object sender, RoutedEventArgs e) => Close();

		public static void Display()
		{
			if (singleton == null)
				singleton = new DateTimeHelpDialog();
			singleton.Show();
			singleton.Focus();
		}
	}

	class DateTimeHelpDialogItem
	{
		public string Char { get; set; }
		public string Description { get; set; }
		public string Example { get; set; }
	}
}
