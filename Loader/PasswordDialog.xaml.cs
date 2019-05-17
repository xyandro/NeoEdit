using System;
using System.Windows;

namespace NeoEdit.Loader
{
	partial class PasswordDialog
	{
		PasswordDialog()
		{
			DataContext = this;
			InitializeComponent();
			Title = ResourceReader.Config.X64Start ?? ResourceReader.Config.X32Start;
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(password.Password))
				return;
			DialogResult = true;
		}

		public static string Run()
		{
			var dialog = new PasswordDialog();
			if (dialog.ShowDialog() != true)
				throw new Exception("Password is required.");
			return dialog.password.Password;
		}
	}
}
