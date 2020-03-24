using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Models;
using NeoEdit.Common.Transform;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class DatabaseConnectDialog
	{
		[DepProp]
		ObservableCollection<DBConnectInfo> DBConnectInfos { get { return UIHelper<DatabaseConnectDialog>.GetPropValue<ObservableCollection<DBConnectInfo>>(this); } set { UIHelper<DatabaseConnectDialog>.SetPropValue(this, value); } }
		[DepProp]
		DBConnectInfo DBConnectInfo { get { return UIHelper<DatabaseConnectDialog>.GetPropValue<DBConnectInfo>(this); } set { UIHelper<DatabaseConnectDialog>.SetPropValue(this, value); } }

		static DatabaseConnectDialog() { UIHelper<DatabaseConnectDialog>.Register(); }
		readonly string dbConfigFile = Path.Combine(Helpers.NeoEditAppData, "DBConfig.xml");

		DatabaseConnectDialog()
		{
			InitializeComponent();
			try { DBConnectInfos = new ObservableCollection<DBConnectInfo>(XMLConverter.FromXML<List<DBConnectInfo>>(XElement.Load(dbConfigFile))); }
			catch { DBConnectInfos = new ObservableCollection<DBConnectInfo>(); }
		}

		void AddClick(object sender, RoutedEventArgs e)
		{
			var result = EditDatabaseConnectDialog.Run(Owner, new DBConnectInfo());
			if (result != null)
				DBConnectInfos.Add(result);
		}

		void CopyClick(object sender, RoutedEventArgs e)
		{
			if (DBConnectInfo == null)
				return;

			var result = EditDatabaseConnectDialog.Run(Owner, DBConnectInfo);
			if (result != null)
				DBConnectInfos.Add(result);
		}

		void EditClick(object sender, RoutedEventArgs e)
		{
			if (DBConnectInfo == null)
				return;

			var result = EditDatabaseConnectDialog.Run(Owner, DBConnectInfo);
			if (result != null)
				DBConnectInfos[DBConnectInfos.IndexOf(DBConnectInfo)] = result;
		}

		void DeleteClick(object sender, RoutedEventArgs e)
		{
			if (DBConnectInfo == null)
				return;

			if (!new Message(this)
			{
				Title = "Confirm",
				Text = "Delete this entry?",
				Options = MessageOptions.YesNoCancel,
				DefaultAccept = MessageOptions.Yes,
				DefaultCancel = MessageOptions.Cancel,
			}.Show().HasFlag(MessageOptions.Yes))
				return;
			DBConnectInfos.Remove(DBConnectInfo);
		}

		void MoveUpClick(object sender, RoutedEventArgs e)
		{
			if (DBConnectInfo == null)
				return;

			var oldIndex = DBConnectInfos.IndexOf(DBConnectInfo);
			var newIndex = Math.Max(0, oldIndex - 1);
			if (oldIndex != newIndex)
				DBConnectInfos.Move(oldIndex, newIndex);
		}

		void MoveDownClick(object sender, RoutedEventArgs e)
		{
			if (DBConnectInfo == null)
				return;

			var oldIndex = DBConnectInfos.IndexOf(DBConnectInfo);
			var newIndex = Math.Min(DBConnectInfos.Count - 1, oldIndex + 1);
			if (oldIndex != newIndex)
				DBConnectInfos.Move(oldIndex, newIndex);
		}

		void TestClick(object sender, RoutedEventArgs e)
		{
			if (DBConnectInfo == null)
				return;

			new Message(this)
			{
				Title = "Information",
				Text = DBConnectInfo.Test() ?? "Connection successful.",
				Options = MessageOptions.Ok,
			}.Show();
		}

		DatabaseConnectDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (DBConnectInfo == null)
				return;

			XMLConverter.ToXML(DBConnectInfos.ToList()).Save(dbConfigFile);

			result = new DatabaseConnectDialogResult { DBConnectInfo = DBConnectInfo };
			DialogResult = true;
		}

		public static DatabaseConnectDialogResult Run(Window parent)
		{
			var dialog = new DatabaseConnectDialog { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
