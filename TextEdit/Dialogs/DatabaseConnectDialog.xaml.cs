using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml.Linq;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class DatabaseConnectDialog
	{
		internal class Result
		{
			public DBConnectInfo DBConnectInfo { get; internal set; }
		}

		[DepProp]
		ObservableCollection<DBConnectInfo> DBConnectInfos { get { return UIHelper<DatabaseConnectDialog>.GetPropValue<ObservableCollection<DBConnectInfo>>(this); } set { UIHelper<DatabaseConnectDialog>.SetPropValue(this, value); } }
		[DepProp]
		DBConnectInfo DBConnectInfo { get { return UIHelper<DatabaseConnectDialog>.GetPropValue<DBConnectInfo>(this); } set { UIHelper<DatabaseConnectDialog>.SetPropValue(this, value); } }

		static DatabaseConnectDialog() { UIHelper<DatabaseConnectDialog>.Register(); }

		readonly string dbConfigFile = Path.Combine(Path.GetDirectoryName(typeof(TextEditTabs).Assembly.Location), "DBConfig.dat");
		const string encryptionKey = "<RSAKeyValue><Modulus>t5X3OepqSDm+YjqiW574OcoYoxLkt4tcEQB7B3+kR9rEVUSUocnQ2pA/rpz91u6crGORz5sUz8rp9NvWhrDeuN/KqZXo8cWGr2l7t6taiAxw+PwQ5f5UhboHOJcUwhqbivABX1yFNTbBZJAUDRNORmY2jhWZ94zVXagQ/FnRulU=</Modulus><Exponent>AQAB</Exponent><P>3qPtMt//q0h6SNUrrC+5DSqxCaCa7d8fQNXJwOdSp3dNX5o8j/MNFRIaJzCvFg6BUAwcpltjNERoKgfR/WoWew==</P><Q>0xf6qI1vTUue8HP4CGZGyItjdslkCIS6eS6MWvjsTdbwELLg13uEYBqM8ppdv0SAdsyZAu8XEx1SDehMwRqBbw==</Q><DP>CX3fnO2jzr+WRwifhgW60+7gAVMRh9adVHxIz6qNAYq6h7rhnhl0k1NkPgt7S2tu4+TAS+9VeWL5NeGDeFRPhQ==</DP><DQ>nw8EepkH8vA2NOzNSlb2owoUyl75l0mb0M/4Rlwmgoign5SJwxR5LIkVB4C1fve47MtByGorsuV2/K+7lg3I1Q==</DQ><InverseQ>qxfKZ8UAQDTevKy3D1b9LQdlqKPRGQPYteecFy3atM14wfWBAQICeSAZJzAdHjor4+r+UPN03ZqvbaLjKdJQkQ==</InverseQ><D>hWEBEyTKPtslBLzQxHwEoAfCSogpf2hSZU/SEqqbslCwn7qJudmkUYbHnZcVnRgS3/QfNZPYVPd5bpphi83ooXFm6z0PCwLXpGIz/Ogl9Ui+E836fpN4OZ5wehdFGZr6RLnpPppP7n1wEQ5lDzX43exjBB/8yGizESUf26E/Myk=</D></RSAKeyValue>";

		DatabaseConnectDialog()
		{
			InitializeComponent();
			try
			{
				var xml = Encoding.UTF8.GetString(Crypto.Decrypt(Crypto.Type.RSAAES, File.ReadAllBytes(dbConfigFile), encryptionKey));
				DBConnectInfos = new ObservableCollection<DBConnectInfo>(XMLConverter.FromXML<List<DBConnectInfo>>(XElement.Parse(xml)));
			}
			catch { DBConnectInfos = new ObservableCollection<DBConnectInfo>(); }
		}

		void AddClick(object sender, RoutedEventArgs e)
		{
			var result = EditDatabaseConnectDialog.Run(Owner, new DBConnectInfo());
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
			if (new Message
			{
				Title = "Confirm",
				Text = "Delete this entry?",
				Options = Message.OptionsEnum.YesNoCancel,
				DefaultAccept = Message.OptionsEnum.Yes,
				DefaultCancel = Message.OptionsEnum.Cancel,
			}.Show() != Message.OptionsEnum.Yes)
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
			new Message
			{
				Title = "Information",
				Text = DBConnectInfo.Test() ?? "Connection successful",
				Options = Message.OptionsEnum.Ok,
				DefaultAccept = Message.OptionsEnum.Ok,
				DefaultCancel = Message.OptionsEnum.Ok,
			}.Show();
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			var data = Encoding.UTF8.GetBytes(XMLConverter.ToXML(DBConnectInfos.ToList()).ToString());
			File.WriteAllBytes(dbConfigFile, Crypto.Encrypt(Crypto.Type.RSAAES, data, encryptionKey));

			result = new Result { DBConnectInfo = DBConnectInfo };
			DialogResult = true;
		}

		public static Result Run(Window parent)
		{
			var dialog = new DatabaseConnectDialog { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
