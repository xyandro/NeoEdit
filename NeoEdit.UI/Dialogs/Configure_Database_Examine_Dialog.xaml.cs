﻿using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Parsing;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Database_Examine_Dialog
	{
		[DepProp]
		public ObservableCollection<string> Collections { get { return UIHelper<Configure_Database_Examine_Dialog>.GetPropValue<ObservableCollection<string>>(this); } set { UIHelper<Configure_Database_Examine_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Collection { get { return UIHelper<Configure_Database_Examine_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Database_Examine_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public DataTable Data { get { return UIHelper<Configure_Database_Examine_Dialog>.GetPropValue<DataTable>(this); } set { UIHelper<Configure_Database_Examine_Dialog>.SetPropValue(this, value); } }

		static Configure_Database_Examine_Dialog()
		{
			UIHelper<Configure_Database_Examine_Dialog>.Register();
			UIHelper<Configure_Database_Examine_Dialog>.AddCallback(a => a.Collection, (obj, o, n) => obj.UpdateData());
		}

		readonly DbConnection dbConnection;
		Configure_Database_Examine_Dialog(DbConnection dbConnection)
		{
			this.dbConnection = dbConnection;
			InitializeComponent();

			Collections = new ObservableCollection<string>(dbConnection.GetSchema().AsEnumerable().Select(row => row["CollectionName"]).Cast<string>());
		}

		void UpdateData() => Data = dbConnection.GetSchema(Collection);

		Configuration_Database_Examine result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Database_Examine();
			DialogResult = true;
		}

		static public Configuration_Database_Examine Run(Window parent, DbConnection dbConnection)
		{
			var dialog = new Configure_Database_Examine_Dialog(dbConnection) { Owner = parent };
			dialog.ShowDialog();
			return dialog.result;
		}
	}
}