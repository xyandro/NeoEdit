using System.Windows;

namespace NeoEdit.GUI.Dialogs
{
	partial class RegExHelpDialog
	{
		static RegExHelpDialog singleton = null;

		RegExHelpDialog()
		{
			InitializeComponent();
		}

		protected override void OnClosed(System.EventArgs e)
		{
			base.OnClosed(e);
			singleton = null;
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			Close();
		}

		public static void Display()
		{
			if (singleton == null)
				singleton = new RegExHelpDialog();
			singleton.Show();
			singleton.Focus();
		}
	}
}
