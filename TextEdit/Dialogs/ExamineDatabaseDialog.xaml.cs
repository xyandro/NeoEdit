using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class ExamineDatabaseDialog
	{
		[DepProp]
		public ObservableCollection<string> Collections { get { return UIHelper<ExamineDatabaseDialog>.GetPropValue<ObservableCollection<string>>(this); } set { UIHelper<ExamineDatabaseDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Collection { get { return UIHelper<ExamineDatabaseDialog>.GetPropValue<string>(this); } set { UIHelper<ExamineDatabaseDialog>.SetPropValue(this, value); } }
		[DepProp]
		public DataTable Data { get { return UIHelper<ExamineDatabaseDialog>.GetPropValue<DataTable>(this); } set { UIHelper<ExamineDatabaseDialog>.SetPropValue(this, value); } }

		static ExamineDatabaseDialog()
		{
			UIHelper<ExamineDatabaseDialog>.Register();
			UIHelper<ExamineDatabaseDialog>.AddCallback(a => a.Collection, (obj, o, n) => obj.UpdateData());
		}

		readonly DbConnection dbConnection;
		ExamineDatabaseDialog(DbConnection dbConnection)
		{
			this.dbConnection = dbConnection;
			InitializeComponent();

			Collections = new ObservableCollection<string>(dbConnection.GetSchema().AsEnumerable().Select(row => row["CollectionName"]).Cast<string>());
		}

		void UpdateData()
		{
			Data = dbConnection.GetSchema(Collection);
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		static public void Run(Window parent, DbConnection dbConnection)
		{
			var dialog = new ExamineDatabaseDialog(dbConnection) { Owner = parent };
			dialog.ShowDialog();
		}
	}
}
