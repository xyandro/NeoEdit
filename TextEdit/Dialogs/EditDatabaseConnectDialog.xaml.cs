using System;
using System.Linq;
using System.Windows;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class EditDatabaseConnectDialog
	{
		readonly DBConnectInfo dbConnectInfo;
		EditDatabaseConnectDialog(DBConnectInfo dbConnectInfo)
		{
			InitializeComponent();
			type.ItemsSource = Enum.GetValues(typeof(DBConnectInfo.DBType)).Cast<DBConnectInfo.DBType>();
			DataContext = this.dbConnectInfo = dbConnectInfo.Copy();
		}

		void TestClick(object sender, RoutedEventArgs e)
		{
			new Message
			{
				Title = "Information",
				Text = dbConnectInfo.Test() ?? "Connection successful",
				Options = Message.OptionsEnum.Ok,
				DefaultAccept = Message.OptionsEnum.Ok,
				DefaultCancel = Message.OptionsEnum.Ok,
			}.Show();
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		public static DBConnectInfo Run(Window parent, DBConnectInfo dbConnectInfo)
		{
			var dialog = new EditDatabaseConnectDialog(dbConnectInfo) { Owner = parent };
			return dialog.ShowDialog() ? dialog.dbConnectInfo : null;
		}
	}
}
