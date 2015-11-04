using System.Windows;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class ExpressionHelpDialog
	{
		static ExpressionHelpDialog singleton = null;

		ExpressionHelpDialog() { InitializeComponent(); }

		protected override void OnClosed(System.EventArgs e)
		{
			singleton = null;
			base.OnClosed(e);
		}

		void OkClick(object sender, RoutedEventArgs e) => Close();

		public static void Display()
		{
			if (singleton == null)
				singleton = new ExpressionHelpDialog();
			singleton.Show();
			singleton.Focus();
		}
	}
}
