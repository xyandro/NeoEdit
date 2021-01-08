using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Transform;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Database_Connect_Dialog
	{
		[DepProp]
		ObservableCollection<DBConnectInfo> DBConnectInfos { get { return UIHelper<Database_Connect_Dialog>.GetPropValue<ObservableCollection<DBConnectInfo>>(this); } set { UIHelper<Database_Connect_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		DBConnectInfo DBConnectInfo { get { return UIHelper<Database_Connect_Dialog>.GetPropValue<DBConnectInfo>(this); } set { UIHelper<Database_Connect_Dialog>.SetPropValue(this, value); } }

		static Database_Connect_Dialog() { UIHelper<Database_Connect_Dialog>.Register(); }
		readonly string dbConfigFile = Path.Combine(Helpers.NeoEditAppData, "DBConfig.xml");

		Database_Connect_Dialog()
		{
			InitializeComponent();
			try { DBConnectInfos = new ObservableCollection<DBConnectInfo>(XMLConverter.FromXML<List<DBConnectInfo>>(XElement.Load(dbConfigFile))); }
			catch { DBConnectInfos = new ObservableCollection<DBConnectInfo>(); }
			DBConnectInfos.CollectionChanged += (s, e) => XMLConverter.ToXML(DBConnectInfos.ToList()).Save(dbConfigFile);
		}

		void AddClick(object sender, RoutedEventArgs e)
		{
			try { DBConnectInfos.Add(EditDatabaseConnectDialog.Run(Owner, new DBConnectInfo())); }
			catch { }
		}

		void CopyClick(object sender, RoutedEventArgs e)
		{
			if (DBConnectInfo == null)
				return;

			try { DBConnectInfos.Add(EditDatabaseConnectDialog.Run(Owner, DBConnectInfo)); }
			catch { }
		}

		void EditClick(object sender, RoutedEventArgs e)
		{
			if (DBConnectInfo == null)
				return;

			try { DBConnectInfos[DBConnectInfos.IndexOf(DBConnectInfo)] = EditDatabaseConnectDialog.Run(Owner, DBConnectInfo); }
			catch { }
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

		Configuration_Database_Connect result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (DBConnectInfo == null)
				return;

			result = new Configuration_Database_Connect { DBConnectInfo = DBConnectInfo };
			DialogResult = true;
		}

		public static Configuration_Database_Connect Run(Window parent)
		{
			var dialog = new Database_Connect_Dialog { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
